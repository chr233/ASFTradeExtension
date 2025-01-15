namespace ASFTradeExtension.Data.Core;

/// <summary>
/// 个人资料数据
/// </summary>
public sealed record FindUserData
{
    public FindUserData(ulong steamId64, string? profilePath)
    {
        SteamId64 = steamId64;
        ProfilePath = profilePath;
    }

    public ulong SteamId64 { get; init; }
    public string? ProfilePath { get; init; }
    public bool IsValid => SteamId64 != 0;
}