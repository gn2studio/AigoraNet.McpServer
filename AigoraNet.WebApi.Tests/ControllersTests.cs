using AigoraNet.Common;
using AigoraNet.Common.Abstracts;
using AigoraNet.Common.CQRS.Files;
using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Controllers;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using Xunit;
using static System.IO.Path;

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

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestObjectLinker : IObjectLinker
    {
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source is Member member && typeof(TDestination) == typeof(MemberDTO))
            {
                return (TDestination)(object)new MemberDTO(member);
            }

            if (source is IEnumerable<Member> members && typeof(TDestination) == typeof(List<MemberDTO>))
            {
                var list = members.Select(m => new MemberDTO(m)).ToList();
                return (TDestination)(object)list;
            }

            return source is TDestination dest ? dest : throw new InvalidOperationException($"Unsupported mapping {typeof(TSource)} -> {typeof(TDestination)}");
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
        {
        }
    }

    private sealed class TestActionBridge : IActionBridge
    {
        private readonly DefaultContext _context;
        private readonly IAzureBlobFileService _blob;

        public TestActionBridge(DefaultContext context, IAzureBlobFileService blob)
        {
            _context = context;
            _blob = blob;
        }

        public Task<TResult> SendAsync<TResult>(IBridgeRequest<TResult> request, CancellationToken ct = default)
        {
            switch (request)
            {
                case CreateMemberCommand createMember:
                    return (Task<TResult>)(object)new CreateMemberCommandHandler(NullLogger<CreateMemberCommandHandler>.Instance, _context).HandleAsync(createMember, ct);
                case GetMemberQuery getMember:
                    return (Task<TResult>)(object)new GetMemberQueryHandler(NullLogger<GetMemberQueryHandler>.Instance, _context).HandleAsync(getMember, ct);
                case CreatePromptTemplateCommand createPromptTemplate:
                    return (Task<TResult>)(object)new CreatePromptTemplateCommandHandler(NullLogger<CreatePromptTemplateCommand>.Instance, _context).HandleAsync(createPromptTemplate, ct);
                case ListPromptTemplatesQuery listPromptTemplates:
                    return (Task<TResult>)(object)new ListPromptTemplatesQueryHandler(_context).HandleAsync(listPromptTemplates, ct);
                case UpsertKeywordPromptCommand upsertKeywordPrompt:
                    return (Task<TResult>)(object)new UpsertKeywordPromptCommandHandler(NullLogger<UpsertKeywordPromptCommand>.Instance, _context).HandleAsync(upsertKeywordPrompt, ct);
                case ListKeywordPromptsQuery listKeywordPrompts:
                    return (Task<TResult>)(object)new ListKeywordPromptsQueryHandler(_context).HandleAsync(listKeywordPrompts, ct);
                case CreateFileMasterCommand createFileMaster:
                    return (Task<TResult>)(object)new CreateFileMasterCommandHandler(NullLogger<CreateFileMasterCommand>.Instance, _context, _blob).HandleAsync(createFileMaster, ct);
                case ReplaceFileMasterCommand replaceFileMaster:
                    return (Task<TResult>)(object)new ReplaceFileMasterCommandHandler(NullLogger<ReplaceFileMasterCommand>.Instance, _context, _blob).HandleAsync(replaceFileMaster, ct);
                case GetFileMasterQuery getFileMaster:
                    return (Task<TResult>)(object)new GetFileMasterQueryHandler(_context).HandleAsync(getFileMaster, ct);
                default:
                    throw new NotSupportedException($"Request type {request.GetType().Name} not supported by TestActionBridge.");
            }
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "AigoraNet.WebApi";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }

    [Fact]
    public async Task MemberController_Create_And_Get()
    {
        using var db = CreateContext(nameof(MemberController_Create_And_Get));
        var bridge = new TestActionBridge(db, new FakeBlob());
        var linker = new TestObjectLinker();
        var controller = new MemberController(NullLogger<MemberController>.Instance, bridge, linker, new FakeEmailSender(), new FakeHostEnvironment());

        var createResult = await controller.Create(
            new CreateMemberCommand("user@example.com", "hash", "nick", null, null),
            CancellationToken.None) as OkObjectResult;

        Assert.NotNull(createResult);
        var created = Assert.IsType<ReturnValues<MemberDTO>>(createResult.Value);
        Assert.True(created.Success);
        Assert.NotNull(created.Data);

        var getResult = await controller.Get(created.Data!.Id, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(getResult);
        var fetched = Assert.IsType<ReturnValues<MemberDTO>>(getResult.Value);
        Assert.True(fetched.Success);
        Assert.Equal("user@example.com", fetched.Data!.Email);
    }

    [Fact]
    public async Task PromptTemplateController_Create_Then_List()
    {
        using var db = CreateContext(nameof(PromptTemplateController_Create_Then_List));
        var bridge = new TestActionBridge(db, new FakeBlob());
        var linker = new TestObjectLinker();
        var controller = new PromptTemplateController(NullLogger<PromptTemplateController>.Instance, bridge, linker);

        var create = await controller.Create(
            new CreatePromptTemplateCommand("Hello", "Hi there", "desc", "en", 1, "actor"),
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(create);
        var created = Assert.IsType<ReturnValues<PromptTemplate>>(create.Value);
        Assert.True(created.Success);

        var list = await controller.List("en", null, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(list);
        var items = Assert.IsType<ReturnValues<List<PromptTemplate>>>(list.Value);
        Assert.True(items.Success);
        Assert.Single(items.Data!);
    }

    [Fact]
    public async Task KeywordPromptController_Upsert_Then_List()
    {
        using var db = CreateContext(nameof(KeywordPromptController_Upsert_Then_List));
        var bridge = new TestActionBridge(db, new FakeBlob());
        var linker = new TestObjectLinker();
        var controller = new KeywordPromptController(NullLogger<KeywordPromptController>.Instance, bridge, linker);
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

        var upsert = await controller.Upsert(
            new UpsertKeywordPromptCommand(null, "hello", template.Id, "en", false, null, "actor"),
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(upsert);
        var upserted = Assert.IsType<ReturnValues<KeywordPrompt>>(upsert.Value);
        Assert.True(upserted.Success);

        var list = await controller.List("en", template.Id, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(list);
        var items = Assert.IsType<ReturnValues<List<KeywordPrompt>>>(list.Value);
        Assert.True(items.Success);
        Assert.Single(items.Data!);
    }

    [Fact]
    public async Task FileController_Create_And_Replace_Disables_Old()
    {
        using var db = CreateContext(nameof(FileController_Create_And_Replace_Disables_Old));
        var blob = new FakeBlob();
        var bridge = new TestActionBridge(db, blob);
        var linker = new TestObjectLinker();
        var controller = new FileController(NullLogger<FileController>.Instance, bridge, linker);

        var create = await controller.Create(
            new CreateFileMasterCommand("file1.txt", 4, "text/plain", new byte[] { 1, 2, 3, 4 }, null, null, "actor"),
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(create);
        var created = Assert.IsType<ReturnValues<FileMaster>>(create.Value);
        Assert.True(created.Success);
        var createdEntity = Assert.IsType<FileMaster>(created.Data);

        var replace = await controller.Replace(
            new ReplaceFileMasterCommand(createdEntity.Id, "file2.txt", 3, "text/plain", new byte[] { 5, 6, 7 }, null, null, "actor"),
            CancellationToken.None) as OkObjectResult;
        Assert.NotNull(replace);
        var replaced = Assert.IsType<ReturnValues<FileMaster>>(replace.Value);
        Assert.True(replaced.Success);
        var replacedEntity = Assert.IsType<FileMaster>(replaced.Data);

        var old = await db.FileMasters.FirstAsync(x => x.Id == createdEntity.Id);
        Assert.False(old.Condition.IsEnabled);
        Assert.Equal(ConditionStatus.Disabled, old.Condition.Status);

        var newEntry = await db.FileMasters.FirstAsync(x => x.Id == replacedEntity.Id);
        Assert.True(newEntry.Condition.IsEnabled);
        Assert.Equal(ConditionStatus.Active, newEntry.Condition.Status);
    }
}
