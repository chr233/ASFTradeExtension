using Newtonsoft.Json;
using SteamKit2;

namespace ASFTradeExtension.Data;

internal sealed record MarketHistoryResponse
{
    [JsonProperty(PropertyName = "success", Required = Required.Always)]
    public bool Success { get; set; }

    [JsonProperty(PropertyName = "pagesize", Required = Required.Always)]
    public int PageSize { get; set; }

    [JsonProperty(PropertyName = "total_count", Required = Required.Always)]
    public int TotalCount { get; set; }

    [JsonProperty(PropertyName = "start", Required = Required.Always)]
    public int Start { get; set; }

    [JsonProperty(PropertyName = "assets", Required = Required.AllowNull)]
    public Dictionary<string, Dictionary<string, Dictionary<string, MarketAssetData>>>? Assets { get; set; }


    public sealed record MarketAssetData
    {
        [JsonProperty(PropertyName = "currency", Required = Required.AllowNull)]
        public ECurrencyCode CurrencyCode { get; set; }

        [JsonProperty(PropertyName = "appid", Required = Required.AllowNull)]
        public uint AppId { get; set; }

        [JsonProperty(PropertyName = "contextid", Required = Required.AllowNull)]
        public uint Contextid { get; set; }

        [JsonProperty(PropertyName = "id", Required = Required.AllowNull)]
        public ulong Id { get; set; }

        [JsonProperty(PropertyName = "classid", Required = Required.AllowNull)]
        public ulong ClassId { get; set; }

        [JsonProperty(PropertyName = "instanceid", Required = Required.AllowNull)]
        public ulong InstanceId { get; set; }

        [JsonProperty(PropertyName = "amount", Required = Required.AllowNull)]
        public uint Amount { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.AllowNull)]
        public byte Status { get; set; }

        [JsonProperty(PropertyName = "tradable", Required = Required.AllowNull)]
        public bool Tradable { get; set; }

        [JsonProperty(PropertyName = "name", Required = Required.AllowNull)]
        public string Name { get; set; } = "";

        [JsonProperty(PropertyName = "name_color", Required = Required.Default)]
        public string? NameColor { get; set; }

        [JsonProperty(PropertyName = "type", Required = Required.AllowNull)]
        public string Type { get; set; } = "";

        [JsonProperty(PropertyName = "market_name", Required = Required.AllowNull)]
        public string MarketName { get; set; } = "";

        [JsonProperty(PropertyName = "market_hash_name", Required = Required.AllowNull)]
        public string MarketHashName { get; set; } = "";

        [JsonProperty(PropertyName = "marketable", Required = Required.AllowNull)]
        public bool Marketable { get; set; }

        [JsonProperty(PropertyName = "actions", Required = Required.Default)]
        public List<MarketAssetActionData>? Actions { get; set; }
    }

    public sealed record MarketAssetActionData
    {
        [JsonProperty(PropertyName = "link", Required = Required.AllowNull)]
        public string Link { get; set; } = "";

        [JsonProperty(PropertyName = "name", Required = Required.AllowNull)]
        public string Name { get; set; } = "";
    }
}
