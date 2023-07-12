using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ASFTradeExtension.Data;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ASFTradeExtension;

internal static class Utils
{
    /// <summary>
    /// 插件配置
    /// </summary>
    internal static PluginConfig Config { get; set; } = new();

    /// <summary>
    /// 更新已就绪
    /// </summary>
    internal static bool UpdatePadding { get; set; }

    /// <summary>
    /// 更新标记
    /// </summary>
    /// <returns></returns>
    private static string UpdateFlag()
    {
        if (UpdatePadding)
        {
            return "*";
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message)
    {
        string flag = UpdateFlag();

        return $"<ASF{flag}> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message)
    {
        string flag = UpdateFlag();

        return $"<{bot.BotName}{flag}> {message}";
    }

    /// <summary>
    /// 获取个人资料链接
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> GetProfileLink(this Bot bot)
    {
        return await bot.ArchiWebHandler.GetAbsoluteProfileURL(true).ConfigureAwait(false);
    }

    /// <summary>
    /// 转换SteamId
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    internal static ulong SteamId2Steam32(ulong steamId)
    {
        return steamId & 0x001111011111111;
    }

    /// <summary>
    /// 转换SteamId
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    internal static ulong Steam322SteamId(ulong steamId)
    {
        return steamId | 0x110000100000000;
    }

    /// <summary>
    /// 获取版本号
    /// </summary>
    internal static Version MyVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0");

    /// <summary>
    /// 获取插件所在路径
    /// </summary>
    internal static string MyLocation => Assembly.GetExecutingAssembly().Location;

    /// <summary>
    /// Steam商店链接
    /// </summary>
    internal static Uri SteamStoreURL => ArchiWebHandler.SteamStoreURL;

    /// <summary>
    /// Steam社区链接
    /// </summary>
    internal static Uri SteamCommunityURL = ArchiWebHandler.SteamCommunityURL;

    /// <summary>
    /// Steam API链接
    /// </summary>
    internal static Uri SteamApiURL => new("https://api.steampowered.com");

    /// <summary>
    /// 日志
    /// </summary>
    internal static ArchiLogger Logger => ASF.ArchiLogger;

    internal static HashSet<T> DistinctList<T>(IEnumerable<T> values)
    {
        var result = new HashSet<T>();
        foreach (var value in values)
        {
            result.Add(value);
        }
        return result;
    }

    internal static HashSet<V> DistinctList<T, V>(IEnumerable<T> values, Func<T, V> selector)
    {
        var result = new HashSet<V>();

        foreach (var value in values)
        {
            result.Add(selector(value));
        }
        return result;
    }

    internal static List<T> OrderLisr<T>(IEnumerable<T> values, bool reverse = false)
    {
        var list = values.ToList();

        if (!reverse)
        {
            list.Sort(Comparer<T>.Default.Compare);
        }
        else
        {
            list.Sort((x, y) => Comparer<T>.Default.Compare(y, x));
        }

        return list;
    }

    internal static List<T> OrderLisr<T, V>(IEnumerable<T> values, Func<T, V> selector, bool reverse = false)
    {
        var list = values.ToList();

        if (!reverse)
        {
            list.Sort((x, y) => Comparer<V>.Default.Compare(selector(x), selector(y)));
        }
        else
        {
            list.Sort((x, y) => Comparer<V>.Default.Compare(selector(y), selector(x)));
        }

        return list;
    }
}
