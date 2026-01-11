using AigoraNet.Common;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.Entities;
using FluentAssertions;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AigoraNet.WebApi.Tests;

public class PromptQueryTests
{
    private static DefaultContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new DefaultContext(options);
    }

    private sealed class FakePromptCache : IPromptCache
    {
        private readonly Dictionary<string, (PromptMatchResult Value, DateTime Expire)> _cache = new();
        public bool TryGet(string key, out PromptMatchResult? value)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expire > DateTime.UtcNow)
            {
                value = entry.Value;
                return true;
            }
            value = null;
            return false;
        }

        public void Set(string key, PromptMatchResult value, TimeSpan ttl)
        {
            _cache[key] = (value, DateTime.UtcNow.Add(ttl));
        }
    }

    private static IPromptCache CreateCache() => new FakePromptCache();

    [Fact]
    public async Task GetPromptByKeyword_ShouldMatchContainsKeyword()
    {
        using var context = CreateContext(nameof(GetPromptByKeyword_ShouldMatchContainsKeyword));
        var cache = CreateCache();

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Greeting",
            Content = "Hello prompt",
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var keyword = new KeywordPrompt
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "hello",
            PromptTemplateId = template.Id,
            PromptTemplate = template,
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        context.PromptTemplates.Add(template);
        context.KeywordPrompts.Add(keyword);
        await context.SaveChangesAsync();

        var result = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery("hello world"),
            context,
            cache,
            NullLogger<GetPromptByKeywordQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Keyword.Should().Be("hello");
        result.Content.Should().Be("Hello prompt");

        // cache hit
        var cached = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery("hello world"),
            context,
            cache,
            NullLogger<GetPromptByKeywordQuery>.Instance,
            CancellationToken.None);
        cached.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPromptByKeyword_ShouldMatchRegex_WhenAllowed()
    {
        using var context = CreateContext(nameof(GetPromptByKeyword_ShouldMatchRegex_WhenAllowed));
        var cache = CreateCache();

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Number",
            Content = "Number prompt",
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var keyword = new KeywordPrompt
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "\\d{3}-\\d{4}",
            IsRegex = true,
            PromptTemplateId = template.Id,
            PromptTemplate = template,
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        context.PromptTemplates.Add(template);
        context.KeywordPrompts.Add(keyword);
        await context.SaveChangesAsync();

        var result = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery("call me at 123-4567"),
            context,
            cache,
            NullLogger<GetPromptByKeywordQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Keyword.Should().Be("\\d{3}-\\d{4}");
        result.Content.Should().Be("Number prompt");
    }

    [Fact]
    public async Task GetPromptByKeyword_ShouldHonorLocale_Filter()
    {
        using var context = CreateContext(nameof(GetPromptByKeyword_ShouldHonorLocale_Filter));
        var cache = CreateCache();

        var templateKo = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "HelloKo",
            Content = "æ»≥Á«œººø‰",
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var templateEn = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "HelloEn",
            Content = "Hello",
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var keywordKo = new KeywordPrompt
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "hello",
            Locale = "ko",
            PromptTemplateId = templateKo.Id,
            PromptTemplate = templateKo,
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var keywordEn = new KeywordPrompt
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "hello",
            Locale = "en",
            PromptTemplateId = templateEn.Id,
            PromptTemplate = templateEn,
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        context.PromptTemplates.AddRange(templateKo, templateEn);
        context.KeywordPrompts.AddRange(keywordKo, keywordEn);
        await context.SaveChangesAsync();

        var resultKo = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery("hello friend", "ko"),
            context,
            cache,
            NullLogger<GetPromptByKeywordQuery>.Instance,
            CancellationToken.None);

        resultKo.Success.Should().BeTrue();
        resultKo.Content.Should().Be("æ»≥Á«œººø‰");

        var resultEn = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery("hello friend", "en"),
            context,
            cache,
            NullLogger<GetPromptByKeywordQuery>.Instance,
            CancellationToken.None);

        resultEn.Success.Should().BeTrue();
        resultEn.Content.Should().Be("Hello");
    }
}
