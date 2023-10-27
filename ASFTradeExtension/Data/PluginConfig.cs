using Newtonsoft.Json;

namespace ASFTradeExtension.Data;

/// <summary>
/// 插件配置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 是否同意使用协议
    /// </summary>
    [JsonProperty(Required = Required.DisallowNull)]
    public bool EULA { get; set; } = true;

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
}
