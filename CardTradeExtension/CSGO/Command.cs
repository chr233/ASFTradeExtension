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
        /// 根据指定交易报价发送指定套数的卡牌
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseSendCSItem(Bot bot, string strAppId, string strSetCount, bool autoConfirm)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            

            if (!uint.TryParse(strAppId, out uint appId) || !uint.TryParse(strSetCount, out uint setCount) || !match.Success)
            {
                return bot.FormatBotResponse(Langs.ArgumentInvalidSCS);
            }

            if (appId == 0 || setCount == 0)
            {
                return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);
            }

            ulong targetSteamId = Steam322SteamId(ulong.Parse(match.Groups[1].Value));
            string tradeToken = match.Groups[2].Value;

            if (!new SteamID(targetSteamId).IsIndividualAccount)
            {
                return bot.FormatBotResponse(Langs.SteamIdInvalid);
            }

            var inventory = await Handler.FetchBotCSInventory(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            }

            //var bundle = await Handler.GetAppCardBundle(bot, appId, inventory).ConfigureAwait(false);

            StringBuilder sb = new();
            //sb.AppendLine(Langs.MultipleLineResult);

            //if (bundle.Assets != null)
            //{
            //    sb.AppendLine(Langs.InventoryStatusBeforeTrade);
            //    sb.AppendLine(
            //        string.Format(Langs.CurrentCardInventoryShow,
            //        appId, bundle.Assets.Count(), bundle.CardCountPerSet,
            //        bundle.TotalSetCount, bundle.ExtraTotalCount,
            //        bundle.TradableSetCount, bundle.ExtraTradableCount)
            //    );

            //    if (bundle.TradableSetCount < setCount)
            //    {
            //        sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            //    }
            //    else
            //    {
            //        List<Asset> offer = new();
            //        var flag = bundle.Assets.Select(x => x.ClassID).Distinct().ToDictionary(x => x, _ => setCount);

            //        foreach (var asset in bundle.Assets)
            //        {
            //            ulong clsId = asset.ClassID;
            //            if (flag[clsId] > 0)
            //            {
            //                offer.Add(asset);
            //                flag[clsId]--;
            //            }
            //        }

            //        if (offer.Any())
            //        {
            //            sb.AppendLine(string.Format(Langs.ExpectToSendCardInfo, setCount, setCount * bundle.CardCountPerSet));
            //            //var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler.SendTradeOffer(targetSteamId, offer, null, tradeToken, false, Config.MaxItemPerTrade).ConfigureAwait(false);

            //            //if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            //            //{
            //            //    (bool twoFactorSuccess, _, _) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

            //            //    sb.AppendLine(string.Format(Langs.TFAConfirmResult, twoFactorSuccess ? Langs.Success : Langs.Failure));

            //            //}

            //            //sb.AppendLine(string.Format(Langs.SendTradeResult, success ? Langs.Success : Langs.Failure));
            //        }
            //        else
            //        {
            //            sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            //        }
            //    }
            //}
            //else
            //{
            //    if (bundle.CardCountPerSet == -1)
            //    {
            //        sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NetworkError));
            //    }
            //    else
            //    {
            //        sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoAvilableCards));
            //    }
            //    sb.AppendLine(Langs.SendTradeFailedAppIdInvalid);
            //}

            return sb.ToString();
        }

        /// <summary>
        /// 根据指定交易报价发送指定套数的卡牌 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseSendCSItem(string botNames, string strAppId, string strSetCount, string tradeLink, bool autoConfirm)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendCardSet(bot, strAppId, strSetCount, tradeLink, autoConfirm))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }
    }
}
