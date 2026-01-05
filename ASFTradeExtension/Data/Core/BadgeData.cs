using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Core;

public sealed record BadgeData
{
    [JsonPropertyName("badgeid")]
    public int Id { get; set; }

    [JsonPropertyName("appid")]
    public uint AppId { get; set; }

    [JsonPropertyName("level")]
    public uint Level { get; set; }

    [JsonPropertyName("border_color")]
    public int BorderColor { get; set; }
}
