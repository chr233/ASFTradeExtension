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

    public string? NickName { get; init; }
    public int CurrentLevel { get; init; }
    public int CurrentExp { get; init; }
    public int TargetLevel { get; init; }
    public int CardSet { get; init; }
    public int CardCount { get; init; }
    public int CardType { get; init; }
    public bool AutoConfirm { get; init; }
}