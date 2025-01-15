using System.Collections.Concurrent;

namespace ASFTradeExtension.Data.Plugin;

/// <summary>
/// 插件数据格式
/// </summary>
public sealed record StorageData
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="masterBotName"></param>
    /// <param name="fullSetCount"></param>
    public StorageData(string? masterBotName, ConcurrentDictionary<uint, int>? fullSetCount)
    {
        MasterBotName = masterBotName;
        FullSetCount = fullSetCount;
    }

    /// <summary>
    /// 发货机器人
    /// </summary>
    public string? MasterBotName { get; init; }

    /// <summary>
    /// 卡牌套数信息
    /// </summary>
    public ConcurrentDictionary<uint, int>? FullSetCount { get; init; }
}