using ArchiSteamFarm.Steam.Data;

namespace ASFTradeExtension.Data;

internal sealed record AssetBundle
{
    /// <summary>
    /// 卡牌列表
    /// </summary>
    public List<Asset> Assets { get; set; } = null!;
    /// <summary>
    /// 一套卡牌的数量, 5~15
    /// </summary>
    public int CardCountPerSet { get; set; }
    /// <summary>
    /// 可交易的套数(不含交易中)
    /// </summary>
    public int TradableSetCount { get; set; }
    /// <summary>
    /// 不可交易套数(不含交易中)
    /// </summary>
    public int NonTradableSetCount { get; set; }

    /// <summary>
    /// 多余可交易张数(不含交易中)
    /// </summary>
    public int ExtraTradableCount { get; set; }
    /// <summary>
    /// 多余不可交易张数(不含交易中)
    /// </summary>
    public int ExtraNonTradableCount { get; set; }
}
