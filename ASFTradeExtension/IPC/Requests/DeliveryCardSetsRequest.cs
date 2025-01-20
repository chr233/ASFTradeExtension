namespace ASFTradeExtension.IPC.Requests;

/// <summary>
/// 发送卡牌请求
/// </summary>
public sealed record DeliveryCardSetsRequest
{
    /// <summary>
    /// 套数信息
    /// </summary>
    public Dictionary<uint, uint>? CardSets { get; set; }

    /// <summary>
    /// 交易链接列表
    /// </summary>
    public string? TradeLink { get; set; }

    public bool FoilCard { get; set; }
    public bool AutoConfirm { get; set; }
}