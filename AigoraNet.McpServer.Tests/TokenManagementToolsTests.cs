using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AigoraNet.Common;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AigoraNet.McpServer.Tests;

public class McpServerFactory : WebApplicationFactory<Program>
{
    private const string TokenKey = "tokentest12345678";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "UseInMemory",
                ["ClientConfiguration:AccessToken"] = "X-Token-Key"
            };
            config.Sources.Clear();
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with InMemory for tests
            services.RemoveAll(typeof(DbContextOptions<DefaultContext>));
            services.RemoveAll(typeof(DefaultContext));

            services.AddDbContext<DefaultContext>(options =>
                options.UseInMemoryDatabase("McpServerTests"));
        });
    }

    public void SeedData()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        Seed(db);
    }

    private static void Seed(DefaultContext db)
    {
        var member = new Member
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@test.com",
            PasswordHash = "hash",
            Condition = new AuditableEntity { CreatedBy = "seed", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active }
        };

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            MemberId = member.Id,
            TokenKey = TokenKey,
            Status = TokenStatus.Issued,
            IssuedAt = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = "seed", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active }
        };

        var prompt = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "hello",
            Content = "Hello prompt",
            Locale = "ko",
            Condition = new AuditableEntity { CreatedBy = "seed", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active }
        };

        var keyword = new KeywordPrompt
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "hello",
            Locale = "ko",
            IsRegex = false,
            PromptTemplateId = prompt.Id,
            Condition = new AuditableEntity { CreatedBy = "seed", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active },
            PromptTemplate = prompt
        };

        var mapping = new TokenPromptMapping
        {
            Id = Guid.NewGuid().ToString(),
            TokenId = token.Id,
            PromptTemplateId = prompt.Id,
            Condition = new AuditableEntity { CreatedBy = "seed", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active },
            PromptTemplate = prompt,
            Token = token
        };

        db.Members.Add(member);
        db.Tokens.Add(token);
        db.PromptTemplates.Add(prompt);
        db.KeywordPrompts.Add(keyword);
        db.TokenPromptMappings.Add(mapping);
        db.SaveChanges();

        if (!db.Tokens.Any(t => t.TokenKey == TokenKey))
        {
            throw new InvalidOperationException("Seed failed: token missing");
        }
    }
}

public class HttpEndpointTests : IClassFixture<McpServerFactory>
{
    private readonly HttpClient _client;
    private readonly McpServerFactory _factory;
    private const string TokenHeader = "X-Token-Key";
    private const string TokenValue = "tokentest12345678";

    public HttpEndpointTests(McpServerFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Remove(TokenHeader);
        var added = _client.DefaultRequestHeaders.TryAddWithoutValidation(TokenHeader, TokenValue);
        added.Should().BeTrue("token header must be added to client");
        factory.SeedData();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingToken_Returns401()
    {
        var response = await _client.GetAsync("/mcp/prompts/tokentest12345678");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PromptsForToken_ReturnsPromptList()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            db.Tokens.Any(t => t.TokenKey == TokenValue).Should().BeTrue("seeded token should exist");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp/prompts/tokentest12345678?token=tokentest12345678");

        var response = await _client.SendAsync(request);
        var bodyText = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {bodyText}");

        var prompts = await response.Content.ReadFromJsonAsync<List<PromptTemplateDTO>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        prompts.Should().NotBeNull();
        prompts!.Should().ContainSingle(p => p.Name == "hello" && p.Content == "Hello prompt");
    }

    [Fact]
    public async Task PromptMatch_ReturnsMatchResult()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp/prompts/match?token=tokentest12345678")
        {
            Content = JsonContent.Create(new GetPromptRequest("hello", "ko", true))
        };

        var response = await _client.SendAsync(request);
        var bodyText = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {bodyText}");

        var result = await response.Content.ReadFromJsonAsync<PromptMatchResult>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PromptName.Should().Be("hello");
        result.Content.Should().Be("Hello prompt");
    }
}
