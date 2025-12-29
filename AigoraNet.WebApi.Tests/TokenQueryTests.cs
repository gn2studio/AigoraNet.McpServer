using AigoraNet.Common;
using AigoraNet.Common.CQRS.Tokens;
using AigoraNet.Common.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AigoraNet.WebApi.Tests;

public class TokenQueryTests
{
    private static DefaultContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new DefaultContext(options);
    }

    [Fact]
    public async Task ListTokensByOwner_ShouldReturnTokensForOwner()
    {
        using var context = CreateContext(nameof(ListTokensByOwner_ShouldReturnTokensForOwner));

        var memberId = Guid.NewGuid().ToString();
        var member = new Member
        {
            Id = memberId,
            Email = "test@example.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var token1 = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "token-key-1",
            MemberId = memberId,
            Name = "Token 1",
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var token2 = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "token-key-2",
            MemberId = memberId,
            Name = "Token 2",
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        // Revoked token should not be included
        var token3 = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "token-key-3",
            MemberId = memberId,
            Name = "Token 3",
            Status = TokenStatus.Revoked,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        context.Members.Add(member);
        context.Tokens.AddRange(token1, token2, token3);
        await context.SaveChangesAsync();

        var result = await TokenQueryHandlers.Handle(
            new ListTokensByOwnerQuery("token-key-1"),
            context,
            NullLogger<ListTokensByOwnerQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Tokens.Should().NotBeNull();
        result.Tokens!.Count.Should().Be(2); // Only token1 and token2, not token3 (revoked)
        result.Tokens.Should().Contain(t => t.Name == "Token 1");
        result.Tokens.Should().Contain(t => t.Name == "Token 2");
        result.Tokens.Should().NotContain(t => t.Name == "Token 3");
        result.Tokens.All(t => t.MaskedTokenKey.Contains("...")).Should().BeTrue();
    }

    [Fact]
    public async Task ListTokensByOwner_ShouldReturnEmptyForNonExistentToken()
    {
        using var context = CreateContext(nameof(ListTokensByOwner_ShouldReturnEmptyForNonExistentToken));

        var result = await TokenQueryHandlers.Handle(
            new ListTokensByOwnerQuery("non-existent-token"),
            context,
            NullLogger<ListTokensByOwnerQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Tokens.Should().NotBeNull();
        result.Tokens!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListTokensByOwner_ShouldReturnErrorForEmptyTokenKey()
    {
        using var context = CreateContext(nameof(ListTokensByOwner_ShouldReturnErrorForEmptyTokenKey));

        var result = await TokenQueryHandlers.Handle(
            new ListTokensByOwnerQuery(""),
            context,
            NullLogger<ListTokensByOwnerQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("TokenKey is required");
    }

    [Fact]
    public async Task GetPromptsForToken_ShouldReturnMappedPrompts()
    {
        using var context = CreateContext(nameof(GetPromptsForToken_ShouldReturnMappedPrompts));

        var memberId = Guid.NewGuid().ToString();
        var member = new Member
        {
            Id = memberId,
            Email = "test@example.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "test-token",
            MemberId = memberId,
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var prompt1 = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Prompt 1",
            Content = "Content 1",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var prompt2 = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Prompt 2",
            Content = "Content 2",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var mapping1 = new TokenPromptMapping
        {
            Id = Guid.NewGuid().ToString(),
            TokenId = token.Id,
            PromptTemplateId = prompt1.Id,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var mapping2 = new TokenPromptMapping
        {
            Id = Guid.NewGuid().ToString(),
            TokenId = token.Id,
            PromptTemplateId = prompt2.Id,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        context.Members.Add(member);
        context.Tokens.Add(token);
        context.PromptTemplates.AddRange(prompt1, prompt2);
        context.TokenPromptMappings.AddRange(mapping1, mapping2);
        await context.SaveChangesAsync();

        var result = await TokenQueryHandlers.Handle(
            new GetPromptsForTokenQuery("test-token"),
            context,
            NullLogger<GetPromptsForTokenQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Prompts.Should().NotBeNull();
        result.Prompts!.Count.Should().Be(2);
        result.Prompts.Should().Contain(p => p.Name == "Prompt 1");
        result.Prompts.Should().Contain(p => p.Name == "Prompt 2");
    }

    [Fact]
    public async Task GetPromptsForToken_ShouldReturnEmptyForTokenWithNoPrompts()
    {
        using var context = CreateContext(nameof(GetPromptsForToken_ShouldReturnEmptyForTokenWithNoPrompts));

        var memberId = Guid.NewGuid().ToString();
        var member = new Member
        {
            Id = memberId,
            Email = "test@example.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "test-token",
            MemberId = memberId,
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        context.Members.Add(member);
        context.Tokens.Add(token);
        await context.SaveChangesAsync();

        var result = await TokenQueryHandlers.Handle(
            new GetPromptsForTokenQuery("test-token"),
            context,
            NullLogger<GetPromptsForTokenQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Prompts.Should().NotBeNull();
        result.Prompts!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPromptsForToken_ShouldReturnErrorForInactiveToken()
    {
        using var context = CreateContext(nameof(GetPromptsForToken_ShouldReturnErrorForInactiveToken));

        var memberId = Guid.NewGuid().ToString();
        var member = new Member
        {
            Id = memberId,
            Email = "test@example.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            TokenKey = "revoked-token",
            MemberId = memberId,
            Status = TokenStatus.Revoked,
            Condition = new AuditableEntity { CreatedBy = "system" }
        };

        context.Members.Add(member);
        context.Tokens.Add(token);
        await context.SaveChangesAsync();

        var result = await TokenQueryHandlers.Handle(
            new GetPromptsForTokenQuery("revoked-token"),
            context,
            NullLogger<GetPromptsForTokenQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Token not found or inactive");
    }

    [Fact]
    public async Task GetPromptsForToken_ShouldReturnErrorForEmptyTokenKey()
    {
        using var context = CreateContext(nameof(GetPromptsForToken_ShouldReturnErrorForEmptyTokenKey));

        var result = await TokenQueryHandlers.Handle(
            new GetPromptsForTokenQuery(""),
            context,
            NullLogger<GetPromptsForTokenQuery>.Instance,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("TokenKey is required");
    }
}
