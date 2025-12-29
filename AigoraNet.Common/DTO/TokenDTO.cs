namespace AigoraNet.Common.DTO;

public record TokenSummaryDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public bool IsEnabled { get; init; }
    // Note: TokenKey is intentionally masked/omitted for security
    public string MaskedTokenKey { get; init; } = string.Empty;
}

public record PromptTemplateDTO
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Version { get; init; }
    public string? Locale { get; init; }
    public bool IsEnabled { get; init; }
}
