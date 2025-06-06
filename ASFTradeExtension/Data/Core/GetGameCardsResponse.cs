using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Core;
internal sealed record GetGameCardsResponse
{
    [JsonPropertyName("result")]
    public Dictionary<uint, int>? Result { get; set; }
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
