using System.Text.RegularExpressions;

namespace ASFTradeExtension;

internal static partial class RegexUtils
{
    public static Regex MatchTradeLink { get; } = GenMatchTradeLink();
    public static Regex MatchCsItemId { get; } = GenMatchCsItemId();
    public static Regex MatchSteamProfileUrl { get; } = GenMatchSteamProfileUrl();
    public static Regex MatchSteamProfileId64 { get; } = GenMatchSteamProfileId64();
    public static Regex MatchSlashRegex { get; } = GenMatchSlashRegex();
    public static Regex MatchGameCards { get; } = GenMatchGameCards();
    public static Regex MatchBadgeLevel { get; } = GenMatchBadgeLevel();
    public static Regex MatchLevelExp { get; } = GenMatchLevelExp();

    [GeneratedRegex(@"(?:https?:\/\/steamcommunity\.com\/tradeoffer\/new\/\?)?partner=(\d+)&token=(\S+)")]
    private static partial Regex GenMatchTradeLink();

    [GeneratedRegex(@"%20M(\d+)A%")]
    private static partial Regex GenMatchCsItemId();

    [GeneratedRegex(@"(?:https?:\/\/steamcommunity\.com\/)?(id\/[^\/]+|profiles\/\d+)\/?")]
    private static partial Regex GenMatchSteamProfileUrl();

    [GeneratedRegex(@"""steamid"":""(\d+)""")]
    private static partial Regex GenMatchSteamProfileId64();

    [GeneratedRegex(@"^\/|\/$")]
    private static partial Regex GenMatchSlashRegex();

    [GeneratedRegex(@"\/gamecards\/(\d+)")]
    private static partial Regex GenMatchGameCards();

    [GeneratedRegex(@"(\d+) 级")]
    private static partial Regex GenMatchBadgeLevel();

    [GeneratedRegex(@"([\d,]+) 点经验值")]
    private static partial Regex GenMatchLevelExp();
}