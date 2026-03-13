using System.Collections.Concurrent;

namespace ASFTradeExtension.Data.Plugin;

/// <summary>
/// 插件数据格式
/// </summary>
public sealed record StorageData
{
    /// <summary>
    /// 发货机器人
    /// </summary>
    public string? MasterBotName { get; init; }

    /// <summary>
    /// 排除的游戏ID列表
    /// </summary>
    public HashSet<uint>? ExcludedAppIds { get; init; }

    /// <summary>
    /// 卡牌套数信息
    /// </summary>
    public ConcurrentDictionary<uint, int>? FullSetCount { get; init; }
}