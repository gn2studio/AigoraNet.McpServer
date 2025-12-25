namespace AigoraNet.Common.DTO;

public class SearchResult
{
	public string Id { get; set; } = string.Empty;

    public string Thumbnail { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string WriterId { get; set; } = string.Empty;

    public DateTime RegistDate { get; set; }
}
