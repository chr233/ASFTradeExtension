using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Integration;
using ASFTradeExtension.Cache;
using ASFTradeExtension.Core;
using ASFTradeExtension.Data.Plugin;
using SteamKit2;
using SteamKit2.Internal;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace ASFTradeExtension;

internal static class Utils
{
    internal static ConcurrentDictionary<Bot, InventoryHandler> CoreHandlers { get; } = new();

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
    /// SteamAPI链接
    /// </summary>
    internal static Uri SteamApiURL => new("https://api.steampowered.com");
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

    /// <summary>
    /// 获取随机从Bot
    /// </summary>
    /// <returns></returns>
    internal static (Bot? bot, InventoryHandler? handler) GetRandomBot()
    {
        var botHandlers = CoreHandlers
            .Where(kv => kv.Key.BotName != CardSetCache.MasterBotName)
            .ToList();

        if (botHandlers.Count > 0)
        {
            var kv = botHandlers[Random.Shared.Next(botHandlers.Count)];
            return (kv.Key, kv.Value);
        }

        return GetMasterBot();
    }

    /// <summary>
    /// 获取主Bot
    /// </summary>
    /// <returns></returns>
    internal static (Bot? bot, InventoryHandler? handler) GetMasterBot()
    {
        if (!string.IsNullOrEmpty(CardSetCache.MasterBotName))
        {
            var bot = Bot.GetBot(CardSetCache.MasterBotName);
            if (bot != null && CoreHandlers.TryGetValue(bot, out var handler))
            {
                return (bot, handler);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// 解析交易链接
    /// </summary>
    /// <param name="tradeLink"></param>
    /// <returns></returns>
    public static (bool valid, ulong steamId64, string? tradeToken) ParseTradeLink(string? tradeLink)
    {
        if (string.IsNullOrEmpty(tradeLink))
        {
            return (false, 0, null);
        }

        var match = RegexUtils.MatchTradeLink.Match(tradeLink);

        if (!match.Success)
        {
            return (false, 0, null);
        }

        var tradeToken = match.Groups[2].Value;
        if (!ulong.TryParse(match.Groups[1].Value, out var steamId32) || string.IsNullOrEmpty(tradeToken))
        {
            return (false, 0, null);
        }

        var steamId64 = Steam322SteamId(steamId32);

        if (!new SteamID(steamId64).IsIndividualAccount)
        {
            return (false, 0, null);
        }

        return (true, steamId64, tradeToken);
    }

    /// <summary>
    /// 计算等级所需要的经验
    /// </summary>
    /// <param name="playerLevel"></param>
    /// <returns></returns>
    private static int CalcLevelExp(int playerLevel)
    {
        var expEveryLevel = 0;
        var totalExp = 0;
        for (var i = 0; i < playerLevel; i++)
        {
            if (i % 10 == 0)
            {
                expEveryLevel += 100;
            }

            totalExp += expEveryLevel;
        }

        return totalExp;
    }

    /// <summary>
    /// 计算达成目标等级所需要的经验
    /// </summary>
    /// <param name="currentLevel"></param>
    /// <param name="targetLevel"></param>
    /// <param name="currentExp"></param>
    /// <returns></returns>
    internal static int CalcExpToLevel(int currentLevel, int targetLevel, int currentExp = 0)
    {
        var nowExp = Math.Max(CalcLevelExp(currentLevel), currentExp);
        var targetExp = CalcLevelExp(targetLevel);
        return targetExp - nowExp;
    }


    /// <summary>
    /// 原子写入文件：写入临时文件，然后使用 File.Replace（存在时）或 File.Move（不存在时）替换目标文件。
    /// 保证在写入过程中不会生成不完整的目标文件（NTFS 上为原子替换）。
    /// </summary>
    internal static async Task WriteFileAtomicAsync(string destinationPath, string content)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Path.GetTempPath();
        }

        Directory.CreateDirectory(directory);

        var tempFile = Path.Combine(directory, $"{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        await File.WriteAllTextAsync(tempFile, content).ConfigureAwait(false);

        try
        {
            if (File.Exists(destinationPath))
            {
                // 当目标存在时，使用 File.Replace 可在多数 NTFS 场景下保证原子替换（并可保留备份）
                File.Replace(tempFile, destinationPath, null);
            }
            else
            {
                // 目标不存在时，直接移动临时文件到目标位置
                File.Move(tempFile, destinationPath);
            }
        }
        catch (Exception ex)
        {
            // 若替换失败，尝试删除临时文件然后抛出异常以便上层记录日志
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch { }

            ASFLogger.LogGenericException(ex);

            throw;
        }
    }
}