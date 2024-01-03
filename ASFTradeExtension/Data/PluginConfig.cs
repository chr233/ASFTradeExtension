using Newtonsoft.Json;

namespace ASFTradeExtension.Data;

/// <summary>
/// 插件配置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 启用统计信息
    /// </summary>
    [JsonProperty(Required = Required.DisallowNull)]
    public bool Statistic { get; set; } = true;

    /// <summary>
    /// 单次交易最大物品数量
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    public ushort MaxItemPerTrade { get; set; } = byte.MaxValue;
    /// <summary>
    /// 缓存生存时间(秒)
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    public ushort CacheTTL { get; set; } = 600;
}
