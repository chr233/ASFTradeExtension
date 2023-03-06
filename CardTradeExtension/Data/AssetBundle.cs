using ArchiSteamFarm.Steam.Data;

namespace CardTradeExtension.Data
{
    internal sealed record AssetBundle
    {
        /// <summary>
        /// 卡牌列表
        /// </summary>
        public IList<Asset>? Assets { get; set; }
        /// <summary>
        /// 一套卡牌的数量, 5~15
        /// </summary>
        public int CardCountPerSet { get; set; }
        /// <summary>
        /// 可交易的套数
        /// </summary>
        public int TradableSetCount { get; set; }
        /// <summary>
        /// 总套数
        /// </summary>
        public int TotalSetCount { get; set; }
        /// <summary>
        /// 多余张数
        /// </summary>
        public int ExtraCount { get; set; }
    }
}
