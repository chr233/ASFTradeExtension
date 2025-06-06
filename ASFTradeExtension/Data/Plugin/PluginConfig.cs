namespace ASFTradeExtension.Data.Plugin;

/// <summary>
/// 插件配置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 是否同意使用协议
    /// </summary>
    public bool EULA { get; init; }

    /// <summary>
    /// 启用统计信息
    /// </summary>
    public bool Statistic { get; init; } = true;

    /// <summary>
    /// 单次交易最大物品数量
    /// </summary>
    public ushort MaxItemPerTrade { get; set; } = byte.MaxValue;

    /// <summary>
    /// 机器人缓存生存时间(秒)
    /// </summary>
    public ushort CacheTTL { get; set; } = 1800;

    /// <summary>
    /// 卡牌信息 API
    /// </summary>
    public string CardsInfoApi { get; init; } = "https://api.1vmp.com";
}