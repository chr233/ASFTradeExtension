namespace ASFTradeExtension.IPC.Responses;

/// <summary>
/// 发送卡牌请求
/// </summary>
public sealed record SendLevelUpSetResponse
{
    public SendLevelUpSetResponse(string? nickName, int currentLevel, int currentExp, int targetLevel, int cardSet,
        int cardCount, int cardType, bool autoConfirm)
    {
        NickName = nickName;
        CurrentLevel = currentLevel;
        CurrentExp = currentExp;
        TargetLevel = targetLevel;
        CardSet = cardSet;
        CardCount = cardCount;
        CardType = cardType;
        AutoConfirm = autoConfirm;
    }

    public string? NickName { get; set; }
    public int CurrentLevel { get; set; }
    public int CurrentExp { get; set; }
    public int TargetLevel { get; set; }
    public int CardSet { get; set; }
    public int CardCount { get; set; }
    public int CardType { get; set; }
    public bool AutoConfirm { get; set; }
}