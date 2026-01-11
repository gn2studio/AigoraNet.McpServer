using System.Text.RegularExpressions;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Prompts;

public record GetPromptByKeywordQuery(string Requirement, string? Locale = null, bool AllowRegex = true) : IBridgeRequest<ReturnValues<PromptMatchResult>>;

public record PromptMatchResult(string? PromptTemplateId, string? PromptName, string? Content, string? Keyword, bool Success, string? Error = null);

public class GetPromptByKeywordQueryHandler : IBridgeHandler<GetPromptByKeywordQuery, ReturnValues<PromptMatchResult>>
{
    private static readonly TimeSpan HitTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MissTtl = TimeSpan.FromMinutes(2);

    private readonly DefaultContext _context;
    private readonly IPromptCache _cache;
    private readonly ILogger<GetPromptByKeywordQuery> _logger;

    public GetPromptByKeywordQueryHandler(ILogger<GetPromptByKeywordQuery> logger, DefaultContext db, IPromptCache cache) : base()
    {
        _context = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ReturnValues<PromptMatchResult>> HandleAsync(GetPromptByKeywordQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<PromptMatchResult>();

        if (string.IsNullOrWhiteSpace(request.Requirement))
        {
            _logger.LogWarning("Prompt match failed: empty requirement");
            result.SetError("Requirement is empty");
            return result;
        }

        var requirement = request.Requirement.Trim();
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale;
        var cacheKey = $"prompt:{locale ?? "*"}:{request.AllowRegex}:{requirement}";

        if (_cache.TryGet(cacheKey, out var cached) && cached is not null)
        {
            _logger.LogInformation("Prompt cache hit for requirement");
            result.SetSuccess(1, cached);
            return result;
        }

        var candidates = await _context.KeywordPrompts
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
            if (candidate.IsRegex && request.AllowRegex)
            {
                if (Regex.IsMatch(requirement, candidate.Keyword, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    var match = new PromptMatchResult(candidate.PromptTemplateId, candidate.TemplateName, candidate.TemplateContent, candidate.Keyword, true);
                    _cache.Set(cacheKey, match, HitTtl);
                    _logger.LogInformation("Matched regex keyword {Keyword}", candidate.Keyword);
                    result.SetSuccess(1, match);
                    return result;
                }
            }
            else
            {
                if (requirement.Contains(candidate.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var match = new PromptMatchResult(candidate.PromptTemplateId, candidate.TemplateName, candidate.TemplateContent, candidate.Keyword, true);
                    _cache.Set(cacheKey, match, HitTtl);
                    _logger.LogInformation("Matched keyword {Keyword}", candidate.Keyword);
                    result.SetSuccess(1, match);
                    return result;
                }
            }
        }

        _logger.LogWarning("No matching prompt for requirement");
        var miss = new PromptMatchResult(null, null, null, null, false, "No matching prompt");
        _cache.Set(cacheKey, miss, MissTtl);
        result.SetError(miss.Error ?? "No matching prompt");
        return result;
    }
}

public static class GetPromptByKeywordHandler
{
    public static async Task<PromptMatchResult> Handle(GetPromptByKeywordQuery query, DefaultContext db, IPromptCache cache, ILogger<GetPromptByKeywordQuery> logger, CancellationToken ct)
    {
        var bridge = await new GetPromptByKeywordQueryHandler(logger, db, cache).HandleAsync(query, ct);
        var data = bridge.Data ?? new PromptMatchResult(null, null, null, null, bridge.Success, bridge.Message);
        return data with { Success = bridge.Success, Error = bridge.Message ?? data.Error };
    }
}
