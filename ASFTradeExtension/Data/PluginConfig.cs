namespace ASFTradeExtension.Data;

/// <summary>
/// 插件配置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 是否同意使用协议
    /// </summary>
    public bool EULA { get; set; }
    /// <summary>
    /// 启用统计信息
    /// </summary>
    public bool Statistic { get; set; } = true;

    /// <summary>
    /// 单次交易最大物品数量
    /// </summary>
    public ushort MaxItemPerTrade { get; set; } = byte.MaxValue;
    /// <summary>
    /// 缓存生存时间(秒)
    /// </summary>
    public ushort CacheTTL { get; set; } = 600;
}
