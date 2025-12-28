using AigoraNet.Common;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AigoraNet.WebApi.Tests;

public class TokenValidationMiddlewareTests
{
    private static DefaultContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new DefaultContext(options);
    }

    [Fact]
    public async Task Middleware_Allows_Request_With_Valid_Token()
    {
        using var context = CreateContext(nameof(Middleware_Allows_Request_With_Valid_Token));

        var member = new Member
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            MemberId = member.Id,
            TokenKey = "valid-token",
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = "tester" }
        };

        context.Members.Add(member);
        context.Tokens.Add(token);
        await context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        httpContext.Request.Headers["X-Token-Key"] = "valid-token";

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new TokenValidationMiddleware(context, NullLogger<TokenValidationMiddleware>.Instance);
        await middleware.InvokeAsync(httpContext, next);

        nextCalled.Should().BeTrue();
        httpContext.Items.Should().ContainKey(TokenValidationMiddleware.HttpContextMemberIdKey);
        httpContext.Items[TokenValidationMiddleware.HttpContextMemberIdKey].Should().Be(member.Id);
    }

    [Fact]
    public async Task Middleware_Rejects_Request_When_Token_Missing()
    {
        using var context = CreateContext(nameof(Middleware_Rejects_Request_When_Token_Missing));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";

        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new TokenValidationMiddleware(context, NullLogger<TokenValidationMiddleware>.Instance);
        await middleware.InvokeAsync(httpContext, next);

        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
