namespace ASFLevelUpBot.Data.Core;
/// <summary>
/// 个人资料数据
/// </summary>
internal sealed record FindUserData
{
    public ulong SteamId64 { get; init; }
    public string? ProfilePath { get; init; }
    public bool IsValid => SteamId64 != 0;

    public FindUserData(ulong steamId64, string? profilePath)
    {
        SteamId64 = steamId64;
        ProfilePath = profilePath;
    }
}