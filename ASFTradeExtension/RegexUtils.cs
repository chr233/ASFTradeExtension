using System.Text.RegularExpressions;

namespace ASFTradeExtension;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"(?:https?:\/\/steamcommunity\.com\/tradeoffer\/new\/\?)?partner=(\d+)&token=(\S+)")]
    public static partial Regex MatchTradeLink();

    [GeneratedRegex(@"%20M(\d+)A%")]
    public static partial Regex MatchCsItemId();
}
