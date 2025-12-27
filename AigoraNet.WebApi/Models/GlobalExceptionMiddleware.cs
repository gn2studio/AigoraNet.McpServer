using AigoraNet.Common.Configurations;
using GN2.Github.Library;
using Serilog;
using System.Net;

namespace AigoraNet.WebApi.Models;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GitHubConfiguration _gitHubConfiguration;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, GitHubConfiguration gitHubConfiguration, IHostEnvironment env)
    {
        _next = next;
        _gitHubConfiguration = gitHubConfiguration;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, GitHubService githubService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 로그 기록
            Log.Error(ex, "Unhandled exception occurred");

            if (_env.IsProduction() && _gitHubConfiguration.IsEnabled)
            {
                // GitHub 이슈 생성
                var issue = new CreateIssueRequestModel
                {
                    Repo = githubService.GetRepositoryName,
                    Owner = githubService.GetOwnerName,
                    Title = $"Exception: {ex.Message}",
                    Body = ex.StackTrace ?? ex.Message,
                    Labels = new[] { "Bug" }
                };

                try
                {
                    await githubService.CreateIssueAsync(issue);
                }
                catch (Exception githubEx)
                {
                    Log.Error(githubEx, "Failed to create GitHub issue");
                }
            }

            // 클라이언트에 응답
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "An unexpected error occurred.",
                detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}