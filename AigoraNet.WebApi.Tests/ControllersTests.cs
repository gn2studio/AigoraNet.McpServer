using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.CQRS.Files;
using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AigoraNet.WebApi.Tests;

public class ControllersTests
{
    private static DefaultContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new DefaultContext(options);
    }

    private sealed class FakeBlob : IAzureBlobFileService
    {
        private readonly Dictionary<string, (byte[] Content, string ContentType)> _store = new();
        public Task DeleteAsync(string blobName, CancellationToken ct)
        {
            _store.Remove(blobName);
            return Task.CompletedTask;
        }

        public Task<BlobUploadResult> UploadAsync(string fileName, byte[] content, string contentType, CancellationToken ct)
        {
            var name = $"blob_{Guid.NewGuid():N}";
            _store[name] = (content, contentType);
            var url = $"https://test/{name}";
            return Task.FromResult(new BlobUploadResult(name, url, content.LongLength, contentType));
        }
    }

    [Fact]
    public async Task MemberController_Create_And_Get()
    {
        using var db = CreateContext(nameof(MemberController_Create_And_Get));
        var controller = new MemberController();

        var createResult = await controller.Create(
            new CreateMemberCommand("user@example.com", "hash", "nick", null, null, Member.MemberType.User, "actor"),
            db,
            NullLogger<CreateMemberCommand>.Instance,
            CancellationToken.None) as OkObjectResult;

        Assert.NotNull(createResult);
        var created = Assert.IsType<Member>(createResult.Value);

        var getResult = await controller.Get(created.Id, db, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(getResult);
        var fetched = Assert.IsType<Member>(getResult.Value);
        Assert.Equal("user@example.com", fetched.Email);
    }

    [Fact]
    public async Task PromptTemplateController_Create_Then_List()
    {
        using var db = CreateContext(nameof(PromptTemplateController_Create_Then_List));
        var controller = new PromptTemplateController();

        var create = await controller.Create(
            new CreatePromptTemplateCommand("Hello", "Hi there", "desc", "en", 1, "actor"),
            db,
            NullLogger<CreatePromptTemplateCommand>.Instance,
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(create);

        var list = await controller.List("en", null, db, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(list);
        var items = Assert.IsAssignableFrom<IEnumerable<PromptTemplate>>(list.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task KeywordPromptController_Upsert_Then_List()
    {
        using var db = CreateContext(nameof(KeywordPromptController_Upsert_Then_List));
        // seed template
        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Greeting",
            Content = "Hello",
            Condition = new AuditableEntity { CreatedBy = "seed" }
        };
        db.PromptTemplates.Add(template);
        db.SaveChanges();

        var controller = new KeywordPromptController();
        var upsert = await controller.Upsert(
            new UpsertKeywordPromptCommand(null, "hello", template.Id, "en", false, null, "actor"),
            db,
            NullLogger<UpsertKeywordPromptCommand>.Instance,
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(upsert);

        var list = await controller.List("en", template.Id, db, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(list);
        var items = Assert.IsAssignableFrom<IEnumerable<KeywordPrompt>>(list.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task FileController_Create_And_Replace_Disables_Old()
    {
        using var db = CreateContext(nameof(FileController_Create_And_Replace_Disables_Old));
        var blob = new FakeBlob();
        var controller = new FileController();

        var create = await controller.Create(
            new CreateFileMasterCommand("file1.txt", 4, "text/plain", new byte[] { 1, 2, 3, 4 }, null, null, "actor"),
            db,
            blob,
            NullLogger<CreateFileMasterCommand>.Instance,
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(create);
        var created = Assert.IsType<FileMaster>(create.Value);

        var replace = await controller.Replace(
            new ReplaceFileMasterCommand(created.Id, "file2.txt", 3, "text/plain", new byte[] { 5, 6, 7 }, null, null, "actor"),
            db,
            blob,
            NullLogger<ReplaceFileMasterCommand>.Instance,
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(replace);
        var replaced = Assert.IsType<FileMaster>(replace.Value);

        var old = await db.FileMasters.FirstAsync(x => x.Id == created.Id);
        Assert.False(old.Condition.IsEnabled);
        Assert.Equal(ConditionStatus.Disabled, old.Condition.Status);

        var newEntry = await db.FileMasters.FirstAsync(x => x.Id == replaced.Id);
        Assert.True(newEntry.Condition.IsEnabled);
        Assert.Equal(ConditionStatus.Active, newEntry.Condition.Status);
    }
}
