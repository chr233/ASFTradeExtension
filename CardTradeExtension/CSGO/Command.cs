using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SteamKit2;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Schema;

namespace CardTradeExtension.CSGO
{
    internal static partial class Command
    {
        /// <summary>
        /// 获取CSGO库存
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseCSItemList(Bot bot, string? query)
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
                    return bot.FormatBotResponse(Langs.ArgumentInvalidFSL);
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

            var inventory = await Handler.FetchBotCSInventory(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            }

            if (!inventory.Any())
            {
                return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
            }

            var classIds = inventory.Select(x => x.ClassID).Distinct();
            var orderedClassIds = classIds.Select(x => (classId: x, XmlSchemaTotalDigitsFacet: inventory.Count(y => y.ClassID == x))).OrderByDescending(z => z.Item2);

            var keys = orderedClassIds.Skip(page * count).Take(count);
            if (!keys.Any())
            {
                return bot.FormatBotResponse(Langs.NoAvilableItemToShow);
            }

            StringBuilder sb = new();
            sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

            foreach (var (classId, total) in keys)
            {
                sb.AppendLine(string.Format(Langs.TwoItem, classId, total));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取CSGO库存 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseCSItemList(string botNames, string? query)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseCSItemList(bot, query))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 获取指定CSGO库存数量
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseCSItemCount(Bot bot, string query)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!queries.Any())
            {
                return bot.FormatBotResponse(Langs.ArgumentInvalidAppIds);
            }

            var classIds = queries.Select(q => uint.TryParse(q, out uint appId) ? appId : 0);

            StringBuilder sb = new();
            sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

            if (classIds.Any())
            {
                var inventory = await Handler.FetchBotCSInventory(bot).ConfigureAwait(false);
                if (inventory == null)
                {
                    return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
                }

                if (!inventory.Any())
                {
                    return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
                }

                foreach (var classId in classIds)
                {
                    int total = inventory.Count(x => x.ClassID == classId);
                    sb.AppendLine(string.Format(Langs.TwoItem, classId, total));
                }
            }
            else
            {
                foreach (var q in queries)
                {
                    sb.AppendLine(string.Format(Langs.TwoItem, q, "无效 ClassId"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取指定CSGO库存数量 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseCSItemCount(string botNames, string query)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseCSItemCount(bot, query))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 发送指定数量的物品到其余账号
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="strClassId"></param>
        /// <param name="strCountPerBot"></param>
        /// <param name="autoConfirm"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseSendCSItem(Bot bot, string? strClassId, string? strCountPerBot, bool autoConfirm)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            var bots = Bot.GetBots("ASF")?.Where(x => x.IsConnectedAndLoggedOn && x != bot);
            if (bots == null || !bots.Any())
            {
                return bot.FormatBotResponse("无可用机器人");
            }

            var tradeToken = await WebRequests.GetTradeToken(bot).ConfigureAwait(false);

            if (string.IsNullOrEmpty(tradeToken))
            {
                return bot.FormatBotResponse("自动获取交易链接失败");
            }


            if (!int.TryParse(strCountPerBot, out int countPerBot))
            {
                if (!string.IsNullOrEmpty(strCountPerBot))
                {
                    return bot.FormatBotResponse("参数无效");
                }
                else
                {
                    countPerBot = 0;
                }
            }

            if (!ulong.TryParse(strClassId, out ulong classId))
            {
                if (!string.IsNullOrEmpty(strClassId))
                {
                    return bot.FormatBotResponse("参数无效");
                }
                else
                {
                    classId = 0;
                }
            }

            var inventory = await Handler.FetchBotCSInventory(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            }
            if (classId != 0)
            {
                inventory = inventory.Where(x => x.ClassID == classId);
            }
            if (!inventory.Any())
            {
                return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
            }

            int invCount = inventory.Count();
            if (countPerBot == 0)
            {
                countPerBot = invCount / bots.Count();
            }

            StringBuilder sb = new();
            sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

            int skip = 0;
            foreach (var b in bots)
            {
                var offer = inventory.Skip(skip).Take(countPerBot).ToList();
                skip += countPerBot;
                if (offer.Any())
                {
                    var (success, tradeOfferIDs, _) = await b.ArchiWebHandler.SendTradeOffer(bot.SteamID, null, offer, tradeToken, false, Config.MaxItemPerTrade).ConfigureAwait(false);

                    if (success && tradeOfferIDs != null && autoConfirm)
                    {
                        foreach (var tradeId in tradeOfferIDs)
                        {
                            Handler.AddTrade(tradeId, b.SteamID);
                        }
                    }
                    sb.AppendLine(string.Format("发送交易报价 {0} -> {1}, 物品数量 {2}, {3}", b.BotName, bot.BotName, offer.Count, success ? Langs.Success : Langs.Failure));
                }
                else
                {
                    sb.AppendLine(string.Format("发送交易报价 {0} -> {1} 失败, 无可用物品", b.BotName, bot.BotName));
                }
                if (skip >= invCount)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 发送指定数量的物品到其余账号 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <param name="strClassId"></param>
        /// <param name="strCountPerBot"></param>
        /// <param name="autoConfirm"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseSendCSItem(string botNames, string? strClassId, string? strCountPerBot, bool autoConfirm)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendCSItem(bot, strClassId, strCountPerBot, autoConfirm))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 批量出售物品
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="strClassId"></param>
        /// <param name="strCountPerBot"></param>
        /// <param name="autoConfirm"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseQuickSellCSItem(Bot bot, string? strClassId, string? strPrice, string? strCOunt, bool autoConfirm)
        {
            //if (!bot.IsConnectedAndLoggedOn)
            //{
            //    return bot.FormatBotResponse(Strings.BotNotConnected);
            //}

            //var bots = Bot.GetBots("ASF")?.Where(x => x.IsConnectedAndLoggedOn && x != bot);
            //if (bots == null || !bots.Any())
            //{
            //    return bot.FormatBotResponse("无可用机器人");
            //}

            //var tradeToken = await WebRequests.GetTradeToken(bot).ConfigureAwait(false);

            //if (string.IsNullOrEmpty(tradeToken))
            //{
            //    return bot.FormatBotResponse("自动获取交易链接失败");
            //}


            //if (!int.TryParse(strCountPerBot, out int countPerBot))
            //{
            //    if (!string.IsNullOrEmpty(strCountPerBot))
            //    {
            //        return bot.FormatBotResponse("参数无效");
            //    }
            //    else
            //    {
            //        countPerBot = 0;
            //    }
            //}

            //if (!ulong.TryParse(strClassId, out ulong classId))
            //{
            //    if (!string.IsNullOrEmpty(strClassId))
            //    {
            //        return bot.FormatBotResponse("参数无效");
            //    }
            //    else
            //    {
            //        classId = 0;
            //    }
            //}

            //var inventory = await Handler.FetchBotCSInventory(bot).ConfigureAwait(false);
            //if (inventory == null)
            //{
            //    return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            //}
            //if (classId != 0)
            //{
            //    inventory = inventory.Where(x => x.ClassID == classId);
            //}
            //if (!inventory.Any())
            //{
            //    return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
            //}

            //int invCount = inventory.Count();
            //if (countPerBot == 0)
            //{
            //    countPerBot = invCount / bots.Count();
            //}

            StringBuilder sb = new();
            //sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

            //int skip = 0;
            //foreach (var b in bots)
            //{
            //    var offer = inventory.Skip(skip).Take(countPerBot).ToList();
            //    skip += countPerBot;
            //    if (offer.Any())
            //    {
            //        var (success, tradeOfferIDs, _) = await b.ArchiWebHandler.SendTradeOffer(bot.SteamID, null, offer, tradeToken, false, Config.MaxItemPerTrade).ConfigureAwait(false);

            //        if (success && tradeOfferIDs != null && autoConfirm)
            //        {
            //            foreach (var tradeId in tradeOfferIDs)
            //            {
            //                Handler.AddTrade(tradeId, b.SteamID);
            //            }
            //        }
            //        sb.AppendLine(string.Format("发送交易报价 {0} -> {1}, 物品数量 {2}, {3}", b.BotName, bot.BotName, offer.Count, success ? Langs.Success : Langs.Failure));
            //    }
            //    else
            //    {
            //        sb.AppendLine(string.Format("发送交易报价 {0} -> {1} 失败, 无可用物品", b.BotName, bot.BotName));
            //    }
            //    if (skip >= invCount)
            //    {
            //        break;
            //    }
            //}

            return sb.ToString();
        }

        /// <summary>
        /// 发送指定数量的物品到其余账号 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <param name="strClassId"></param>
        /// <param name="strCountPerBot"></param>
        /// <param name="autoConfirm"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseASendCSItem(string botNames, string? strClassId, string? strCountPerBot, bool autoConfirm)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendCSItem(bot, strClassId, strCountPerBot, autoConfirm))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }
    }
}
