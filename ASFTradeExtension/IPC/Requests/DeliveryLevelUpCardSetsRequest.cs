namespace ASFTradeExtension.IPC.Requests;

public sealed record DeliveryLevelUpCardSetsRequest
{
    public int TargetLevel { get; set; }

    public string? TradeLink { get; set; }

    public bool AutoConfirm { get; set; }
}
