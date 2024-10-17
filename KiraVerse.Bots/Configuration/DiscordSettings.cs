namespace KiraVerse.Bots.Configuration;

public record DiscordSettings
{
    public string? SalesBotToken { get; set; }
    public string? SalesBotChannel { get; set; }
}