using System.Security.Claims;
using AigoraNet.Common;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Middleware;
using GN2.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AigoraNet.WebApi.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var httpContext = context.HttpContext;
        var memberId = ResolveMemberId(httpContext);

        if (string.IsNullOrWhiteSpace(memberId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var db = httpContext.RequestServices.GetRequiredService<DefaultContext>();
        var isAdmin = await db.Members
            .AsNoTracking()
            .Where(m => m.Id == memberId)
            .Where(m => m.Condition.IsEnabled && m.Condition.Status == ConditionStatus.Active)
            .Select(m => m.Type == Member.MemberType.Admin)
            .FirstOrDefaultAsync(httpContext.RequestAborted);

        if (!isAdmin)
        {
            context.Result = new ForbidResult();
        }
    }

    private static string? ResolveMemberId(HttpContext context)
    {
        if (context.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj)
            && memberIdObj is string memberId
            && !string.IsNullOrWhiteSpace(memberId))
        {
            return memberId;
        }

        var claim = context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? context.User?.FindFirst("sub");
        return string.IsNullOrWhiteSpace(claim?.Value) ? null : claim!.Value;
    }
}
