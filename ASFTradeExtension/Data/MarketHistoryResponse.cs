using SteamKit2;
using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data;

internal sealed record MarketHistoryResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("pagesize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("assets")]
    public Dictionary<string, Dictionary<string, Dictionary<string, MarketAssetData>>>? Assets { get; set; }


    public sealed record MarketAssetData
    {
        [JsonPropertyName("currency")]
        public ECurrencyCode CurrencyCode { get; set; }

        [JsonPropertyName("appid")]
        public uint AppId { get; set; }

        [JsonPropertyName("contextid")]
        public uint Contextid { get; set; }

        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("classid")]
        public ulong ClassId { get; set; }

        [JsonPropertyName("instanceid")]
        public ulong InstanceId { get; set; }

        [JsonPropertyName("amount")]
        public uint Amount { get; set; }

        [JsonPropertyName("status")]
        public byte Status { get; set; }

        [JsonPropertyName("tradable")]
        public bool Tradable { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("name_color")]
        public string? NameColor { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("market_name")]
        public string MarketName { get; set; } = "";

        [JsonPropertyName("market_hash_name")]
        public string MarketHashName { get; set; } = "";

        [JsonPropertyName("marketable")]
        public bool Marketable { get; set; }

        [JsonPropertyName("actions")]
        public List<MarketAssetActionData>? Actions { get; set; }
    }

    public sealed record MarketAssetActionData
    {
        [JsonPropertyName("link")]
        public string Link { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
