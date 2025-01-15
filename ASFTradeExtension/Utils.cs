using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Integration;
using ASFTradeExtension.Cache;
using ASFTradeExtension.Data.Plugin;
using SteamKit2.Internal;
using System.Reflection;
using System.Text;

namespace ASFTradeExtension;

internal static class Utils
{
    /// <summary>
    /// 促销卡牌
    /// </summary>
    internal static uint SaleEventAppId = 2861690;

    /// <summary>
    /// 插件配置
    /// </summary>
    internal static PluginConfig Config { get; set; } = new();

    internal static CardSetManager CardSetCache { get; } = new();

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
    internal static Uri SteamCommunityURL => ArchiWebHandler.SteamCommunityURL;

    /// <summary>
    /// 日志
    /// </summary>
    internal static ArchiLogger ASFLogger => ASF.ArchiLogger;

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message)
    {
        return $"<ASF> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message, params object?[] args)
    {
        return FormatStaticResponse(string.Format(message, args));
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message)
    {
        return $"<{bot.BotName}> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message, params object?[] args)
    {
        return bot.FormatBotResponse(string.Format(message, args));
    }

    internal static StringBuilder AppendLineFormat(this StringBuilder sb, string format, params object?[] args)
    {
        return sb.AppendLine(string.Format(format, args));
    }

    /// <summary>
    /// 获取个人资料链接
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> GetProfileLink(this Bot bot)
    {
        return await bot.ArchiWebHandler.GetAbsoluteProfileURL().ConfigureAwait(false);
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

    internal static CEcon_Asset CopyWithAmount(this CEcon_Asset body, ulong newAmount)
    {
        if (newAmount > long.MaxValue)
        {
            newAmount = long.MaxValue;
        }

        var newBody = new CEcon_Asset
        {
            amount = (long)newAmount,
            appid = body.appid,
            assetid = body.assetid,
            classid = body.classid,
            contextid = body.contextid,
            currencyid = body.currencyid,
            est_usd = body.est_usd,
            instanceid = body.instanceid,
            missing = body.missing
        };

        return newBody;
    }

    internal static Asset CopyWithAmount(this Asset asset, ulong newAmount)
    {
        var newBody = asset.Body.CopyWithAmount(newAmount);
        var newAsset = new Asset(newBody, asset.Description);
        return newAsset;
    }
}