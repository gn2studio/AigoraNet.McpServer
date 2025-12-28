using System.Text.RegularExpressions;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Prompts;

public record GetPromptByKeywordQuery(string Requirement, string? Locale = null, bool AllowRegex = true);

public record PromptMatchResult(string? PromptTemplateId, string? PromptName, string? Content, string? Keyword, bool Success, string? Error = null);

public static class GetPromptByKeywordHandler
{
    private static readonly TimeSpan HitTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MissTtl = TimeSpan.FromMinutes(2);

    public static async Task<PromptMatchResult> Handle(GetPromptByKeywordQuery query, DefaultContext db, IPromptCache cache, ILogger<GetPromptByKeywordQuery> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Requirement))
        {
            logger.LogWarning("Prompt match failed: empty requirement");
            return new PromptMatchResult(null, null, null, null, false, "Requirement is empty");
        }

        var requirement = query.Requirement.Trim();
        var locale = string.IsNullOrWhiteSpace(query.Locale) ? null : query.Locale;
        var cacheKey = $"prompt:{locale ?? "*"}:{query.AllowRegex}:{requirement}";

        if (cache.TryGet(cacheKey, out var cached) && cached is not null)
        {
            logger.LogInformation("Prompt cache hit for requirement");
            return cached;
        }

        var candidates = await db.KeywordPrompts
            .AsNoTracking()
            .Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .Where(x => locale == null || x.Locale == locale)
            .Include(x => x.PromptTemplate)
            .Select(x => new
            {
                x.Keyword,
                x.IsRegex,
                x.PromptTemplateId,
                TemplateName = x.PromptTemplate!.Name,
                TemplateContent = x.PromptTemplate.Content
            })
            .OrderByDescending(x => x.Keyword.Length)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            if (candidate.IsRegex && query.AllowRegex)
            {
                if (Regex.IsMatch(requirement, candidate.Keyword, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    var match = new PromptMatchResult(candidate.PromptTemplateId, candidate.TemplateName, candidate.TemplateContent, candidate.Keyword, true);
                    cache.Set(cacheKey, match, HitTtl);
                    logger.LogInformation("Matched regex keyword {Keyword}", candidate.Keyword);
                    return match;
                }
            }
            else
            {
                if (requirement.Contains(candidate.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var match = new PromptMatchResult(candidate.PromptTemplateId, candidate.TemplateName, candidate.TemplateContent, candidate.Keyword, true);
                    cache.Set(cacheKey, match, HitTtl);
                    logger.LogInformation("Matched keyword {Keyword}", candidate.Keyword);
                    return match;
                }
            }
        }

        logger.LogWarning("No matching prompt for requirement");
        var miss = new PromptMatchResult(null, null, null, null, false, "No matching prompt");
        cache.Set(cacheKey, miss, MissTtl);
        return miss;
    }
}
