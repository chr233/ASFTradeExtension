using ArchiSteamFarm.Steam.Data;

namespace ASFTradeExtension.Data;
internal sealed record GemsInfo
{
    public uint TradableBags { get; set; }
    public uint NonTradableBags { get; set; }
    public ulong TradableGems { get; set; }
    public ulong NonTradableGems { get; set; }
    public List<Asset> Assets { get; } = [];
    public List<Asset> Assets { get; } = [];
}
