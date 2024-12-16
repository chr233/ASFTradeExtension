namespace ASFTradeExtension.IPC.Responses;

/// <summary>
///     发送卡牌请求
/// </summary>
public sealed record SendCardSetResponse
{
    /// <summary>
    ///     交易链接列表
    /// </summary>
    public string? TradeLink { get; set; }

    public bool FoilCard { get; set; }

    public uint SetCount { get; set; }
    public uint CardCount { get; set; }
    public bool AutoConfirm { get; set; }
}