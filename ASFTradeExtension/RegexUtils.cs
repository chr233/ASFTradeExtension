using System.Text.RegularExpressions;

namespace ASFTradeExtension;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"(?:https?:\/\/steamcommunity\.com\/tradeoffer\/new\/\?)?partner=(\d+)&token=(\S+)")]
    private static partial Regex GenMatchTradeLink();
    public static Regex MatchTradeLink = GenMatchTradeLink();

    [GeneratedRegex(@"%20M(\d+)A%")]
    private static partial Regex GenMatchCsItemId();
    public static Regex MatchCsItemId = GenMatchCsItemId();

    [GeneratedRegex(@"(?:https?:\/\/steamcommunity\.com\/)?(id\/[^\/]+|profiles\/\d+)\/?")]
    private static partial Regex GenMatchSteamProfileUrl();
    public static Regex MatchSteamProfileUrl = GenMatchSteamProfileUrl();

    [GeneratedRegex(@"""steamid"":""(\d+)""")]
    private static partial Regex GenMatchSteamProfileId64();
    public static Regex MatchSteamProfileId64 = GenMatchSteamProfileId64();

    [GeneratedRegex(@"^\/|\/$")]
    private static partial Regex GenMatchSlashRegex();
    public static Regex MatchSlashRegex = GenMatchSlashRegex();

    [GeneratedRegex(@"\/gamecards\/(\d+)")]
    private static partial Regex GenMatchGameCards();
    public static Regex MatchGameCards = GenMatchGameCards();

    [GeneratedRegex(@"(\d+) 级")]
    private static partial Regex GenMatchBadgeLevel();
    public static Regex MatchBadgeLevel = GenMatchBadgeLevel();

    [GeneratedRegex(@"([\d,]+) 点经验值")]
    private static partial Regex GenMatchLevelExp();
    public static Regex MatchLevelExp = GenMatchLevelExp();
}