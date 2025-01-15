using ArchiSteamFarm.Steam.Data;

namespace ASFTradeExtension.Data.Core;

internal sealed record GemsInfo
{
    public List<Asset> GemAssets { get; set; } = [];
    public List<Asset> BagAssets { get; set; } = [];
}