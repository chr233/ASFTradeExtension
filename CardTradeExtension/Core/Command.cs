using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using Microsoft.AspNetCore.Mvc;
using SteamKit2;
using SteamKit2.GC.Dota.Internal;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using static SteamKit2.GC.Underlords.Internal.CUserMessageVGUIMenu;

namespace CardTradeExtension.Core
{
    internal static partial class Command
    {
        /// <summary>
        /// 获取成套卡牌套数列表
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseCardSetList(Bot bot, string? query)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            int page = 0;
            int count = 20;

            if (!string.IsNullOrEmpty(query))
            {
                var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (queries.Length % 2 != 0)
                {
                    return bot.FormatBotResponse("参数有误, 示例: -p 2 -l 20 (第二页, 每页20条)");
                }
                for (int i = 0; i < queries.Length; i += 2)
                {
                    string option = queries[i].ToLowerInvariant();
                    string value = queries[i + 1];

                    switch (option)
                    {
                        case "-p":
                        case "-page":
                            if (int.TryParse(value, out var p) && p > 0)
                            {
                                page = p;
                            }
                            continue;
                        case "-l":
                        case "-line":
                            if (int.TryParse(value, out var l) && l > 0)
                            {
                                count = l;
                            }
                            continue;
                    }
                }
            }

            var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse("网络异常, 读取库存信息失败");
            }

            if (!inventory.Any())
            {
                return bot.FormatBotResponse("卡片库存为空");
            }

            var appIds = inventory.Select(x => x.RealAppID).Distinct().OrderBy(x => inventory.Count(y => y.RealAppID == x)).Reverse();
            var keys = appIds.Skip(page * count).Take(count);
            if (!keys.Any())
            {
                return bot.FormatBotResponse("当前设置下无可显示的内容");
            }

            var cardGroup = await Handler.GetAppCardGroup(bot, appIds, inventory).ConfigureAwait(false);

            StringBuilder sb = new();
            sb.AppendLine(Langs.MultipleLineResult);

            foreach (uint appId in keys)
            {
                if (cardGroup.TryGetValue(appId, out var bundle))
                {
                    if (bundle.Assets != null)
                    {
                        sb.AppendLine(
                            string.Format("{0}: 总计 {1}张, 每套 {2}张, 总计 {3}套 + {4}张, 可交易 {5}套 +{6}张",
                            appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                            bundle.TotalSetCount, bundle.ExtraTotalCount,
                            bundle.TradableSetCount, bundle.ExtraTradableCount)
                        );
                    }
                    else
                    {
                        if (bundle.CardCountPerSet == -1)
                        {
                            sb.AppendLine(string.Format("{0}: {1}", appId, "网络错误"));
                        }
                        else
                        {
                            sb.AppendLine(string.Format("{0}: {1}", appId, "无卡牌"));
                        }
                    }
                }
                else
                {
                    sb.AppendLine(string.Format("{0}: {1}", appId, "无信息"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取成套卡牌套数 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseCardSetList(string botNames, string? query)
        {
            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if (bots == null || bots.Count == 0)
            {
                return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseCardSetList(bot, query))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 获取指定游戏成套卡牌套数
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseGetCardSetCountOfGame(Bot bot, string query)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!queries.Any())
            {
                return bot.FormatBotResponse("输入参数 AppIds 无效");
            }

            var appIds = queries.Select(q => uint.TryParse(q, out uint appId) ? appId : 0);

            StringBuilder sb = new();
            sb.AppendLine(Langs.MultipleLineResult);

            if (appIds.Any())
            {
                var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
                if (inventory == null)
                {
                    return bot.FormatBotResponse("网络异常, 读取库存信息失败");
                }

                if (!inventory.Any())
                {
                    return bot.FormatBotResponse("卡片库存为空");
                }

                var cardGroup = await Handler.GetAppCardGroup(bot, appIds, inventory).ConfigureAwait(false);

                int i = 0;
                foreach (uint appId in appIds)
                {
                    if (appId == 0)
                    {
                        sb.AppendLine(string.Format("{0}: {1}", queries[i], "无效 AppId"));
                    }
                    else
                    {
                        if (cardGroup.TryGetValue(appId, out var bundle))
                        {
                            if (bundle.Assets != null)
                            {
                                sb.AppendLine(
                                    string.Format("{0}: 总计 {1}张, 每套 {2}张, 总计 {3}套 + {4}张, 可交易 {5}套 +{6}张",
                                    appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                                    bundle.TotalSetCount, bundle.ExtraTotalCount,
                                    bundle.TradableSetCount, bundle.ExtraTradableCount)
                                );
                            }
                            else
                            {
                                if (bundle.CardCountPerSet == -1)
                                {
                                    sb.AppendLine(string.Format("{0}: {1}", appId, "网络错误"));
                                }
                                else
                                {
                                    sb.AppendLine(string.Format("{0}: {1}", appId, "无卡牌"));
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine(string.Format("{0}: {1}", appId, "无信息"));
                        }
                    }
                    i++;
                }
            }
            else
            {
                foreach (var q in queries)
                {
                    sb.AppendLine(string.Format("{0}: {1}", q, "无效 AppId"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取指定游戏成套卡牌套数 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseGetCardSetCountOfGame(string botNames, string query)
        {
            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if (bots == null || bots.Count == 0)
            {
                return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGetCardSetCountOfGame(bot, query))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }


        [GeneratedRegex("(?:https?:\\/\\/steamcommunity\\.com\\/tradeoffer\\/new\\/\\?)?partner=(\\d+)&token=(\\S+)")]
        private static partial Regex MatchTradeLink();

        /// <summary>
        /// 根据指定交易报价发送指定套数的卡牌
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseSendCardSet(Bot bot, string strAppId, string strSetCount, string tradeLink)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            var match = MatchTradeLink().Match(tradeLink);

            if (!uint.TryParse(strAppId, out uint appId) || !uint.TryParse(strSetCount, out uint setCount) || !match.Success)
            {
                return bot.FormatBotResponse("参数无效, 示例 730 2 交易链接");
            }

            if (appId == 0 || setCount == 0)
            {
                return bot.FormatBotResponse("AppId 和 SetCount 必须大于0");
            }

            ulong targetSteamId = ulong.Parse(match.Groups[1].Value);
            string tradeToken = match.Groups[2].Value;

            if (!new SteamID(targetSteamId).IsIndividualAccount)
            {
                return bot.FormatBotResponse("SteamId 无效");
            }

            var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse("网络异常, 读取库存信息失败");
            }

            var bundle = await Handler.GetAppCardBundle(bot, appId, inventory).ConfigureAwait(false);

            StringBuilder sb = new();
            sb.AppendLine(Langs.MultipleLineResult);

            if (bundle.Assets != null)
            {
                sb.AppendLine("交易前库存状态:");
                sb.AppendLine(
                    string.Format("{0}: 总计 {1}张, 每套 {2}张, 总计 {3}套 + {4}张, 可交易 {5}套 +{6}张",
                    appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                    bundle.TotalSetCount, bundle.ExtraTotalCount,
                    bundle.TradableSetCount, bundle.ExtraTradableCount)
                );

                if (bundle.TradableSetCount < setCount)
                {
                    sb.AppendLine("交易报价发送成功失败, 可交易卡牌数量不足");
                }
                else
                {
                    List<Asset> offer = new();
                    var flag = bundle.Assets.Select(x => x.ClassID).Distinct().ToDictionary(x => x, _ => setCount);

                    foreach (var asset in bundle.Assets)
                    {
                        ulong clsId = asset.ClassID;
                        if (flag[clsId] > 0)
                        {
                            offer.Add(asset);
                            flag[clsId]--;
                        }
                    }

                    if (offer.Any())
                    {
                        var (success, _, _) = await bot.ArchiWebHandler.SendTradeOffer(targetSteamId, offer, null, tradeToken, false, byte.MaxValue).ConfigureAwait(false);
                        sb.AppendLine(success ? "交易报价发送成功" : "交易报价发送失败");
                    }
                    else
                    {
                        sb.AppendLine("交易报价发送成功失败, 可交易卡牌数量不足");
                    }
                }
            }
            else
            {
                if (bundle.CardCountPerSet == -1)
                {
                    sb.AppendLine(string.Format("{0}: {1}", appId, "网络错误"));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}: {1}", appId, "无卡牌"));
                }
                sb.AppendLine("发送交易失败, AppId 可能无效");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 根据指定交易报价发送指定套数的卡牌 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseSendCardSet(string botNames, string strAppId, string strSetCount, string tradeLink)
        {
            if (string.IsNullOrEmpty(botNames))
            {
                throw new ArgumentNullException(nameof(botNames));
            }

            HashSet<Bot>? bots = Bot.GetBots(botNames);

            if (bots == null || bots.Count == 0)
            {
                return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
            }

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendCardSet(bot, strAppId, strSetCount, tradeLink))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

    }
}
