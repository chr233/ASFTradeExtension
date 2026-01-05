using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Core;

public sealed record GetBadgesResponse
{
    [JsonPropertyName("response")]
    public ResponseData? Response { get; set; }
    public sealed record ResponseData
    {
        [JsonPropertyName("badges")]
        public List<BadgeData>? Badges { get; set; }
    }
}
