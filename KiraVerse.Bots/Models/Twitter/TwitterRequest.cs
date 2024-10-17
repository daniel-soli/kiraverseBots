namespace KiraVerse.Bots.Models.Twitter;

public record TwitterRequest
{
    public string? Text { get; set; }
    public TwitterMedia? Media { get; set; }
}

public record TwitterMedia
{
    public string[]? Media_ids { get; set; }
}