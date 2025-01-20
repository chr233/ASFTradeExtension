using ArchiSteamFarm.Steam.Data;
using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Core;

public sealed record AssetBundle
{
    public AssetBundle(uint appId, int cardCountPerSet)
    {
        Assets = [];
        AppId = appId;
        CardCountPerSet = cardCountPerSet;
        TradableSetCount = 0;
        ExtraTradableCount = 0;
        ExtraNonTradableCount = 0;
    }

    /// <summary>
    /// 卡牌列表
    /// </summary>
    [JsonIgnore]
    internal List<Asset> Assets { get; set; }

    /// <summary>
    /// AppId
    /// </summary>
    public uint AppId { get; set; }

    /// <summary>
    /// 是否已加载
    /// </summary>
    public bool Loaded => CardCountPerSet > 0;

    /// <summary>
    /// 一套卡牌的数量, 5~15
    /// </summary>
    public int CardCountPerSet { get; set; }

    /// <summary>
    /// 可交易的套数(不含交易中)
    /// </summary>
    public int TradableSetCount { get; set; }

    /// <summary>
    /// 多余可交易张数(不含交易中)
    /// </summary>
    public int ExtraTradableCount { get; set; }

    /// <summary>
    /// 多余不可交易张数(不含交易中)
    /// </summary>
    public int ExtraNonTradableCount { get; set; }
}