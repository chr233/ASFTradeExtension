using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using SteamKit2;
using System.Text;

namespace ASFTradeExtension.Card;

internal static class Command
{
    /// <summary>
    /// 获取成套卡牌套数列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseFullSetList(Bot bot, string? query)
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

        var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
        if (inventory == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }

        if (!inventory.Any())
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }

        var appIds = Utils.OrderLisr(Utils.DistinctList(inventory, x => x.RealAppID)
            .OrderByDescending(x => inventory.Count(y => y.RealAppID == x)));

        var keys = appIds.Skip(page * count).Take(count);
        if (!keys.Any())
        {
            return bot.FormatBotResponse(Langs.NoAvailableItemToShow);
        }

        var cardGroup = await Handler.GetAppCardGroup(bot, appIds, inventory).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        foreach (uint appId in keys)
        {
            if (cardGroup.TryGetValue(appId, out var bundle))
            {
                if (bundle.Assets != null)
                {
                    sb.AppendLine(
                        string.Format(Langs.CurrentCardInventoryShow,
                        appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                        bundle.TotalSetCount, bundle.ExtraTotalCount,
                        bundle.TradableSetCount, bundle.ExtraTradableCount)
                    );
                }
                else
                {
                    if (bundle.CardCountPerSet == -1)
                    {
                        sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NetworkError));
                    }
                    else
                    {
                        sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoAvailableCards));
                    }
                }
            }
            else
            {
                sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoInformation));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取成套卡牌套数 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseFullSetList(string botNames, string? query)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return Utils.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseFullSetList(bot, query))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取指定游戏成套卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseFullSetCountOfGame(Bot bot, string query)
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

        var appIds = queries.Select(q => uint.TryParse(q, out uint appId) ? appId : 0);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (appIds.Any())
        {
            var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            }

            if (!inventory.Any())
            {
                return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
            }

            var cardGroup = await Handler.GetAppCardGroup(bot, appIds, inventory).ConfigureAwait(false);

            int i = 0;
            foreach (uint appId in appIds)
            {
                if (appId == 0)
                {
                    sb.AppendLine(string.Format(Langs.TwoItem, queries[i], Langs.AppIdInvalid));
                }
                else
                {
                    if (cardGroup.TryGetValue(appId, out var bundle))
                    {
                        if (bundle.Assets != null)
                        {
                            sb.AppendLine(
                                string.Format(Langs.CurrentCardInventoryShow,
                                appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                                bundle.TotalSetCount, bundle.ExtraTotalCount,
                                bundle.TradableSetCount, bundle.ExtraTradableCount)
                            );
                        }
                        else
                        {
                            if (bundle.CardCountPerSet == -1)
                            {
                                sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NetworkError));
                            }
                            else
                            {
                                sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoAvailableCards));
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoInformation));
                    }
                }
                i++;
            }
        }
        else
        {
            foreach (var q in queries)
            {
                sb.AppendLine(string.Format(Langs.TwoItem, q, Langs.AppIdInvalid));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取指定游戏成套卡牌套数 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseFullSetCountOfGame(string botNames, string query)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return Utils.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseFullSetCountOfGame(bot, query))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 根据指定交易报价发送指定套数的卡牌
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strAppId"></param>
    /// <param name="strSetCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSendCardSet(Bot bot, string strAppId, string strSetCount, string tradeLink, bool autoConfirm)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        var match = RegexUtils.MatchTradeLink().Match(tradeLink);

        if (!uint.TryParse(strAppId, out uint appId) || !uint.TryParse(strSetCount, out uint setCount) || !match.Success)
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS);
        }

        if (appId == 0 || setCount == 0)
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);
        }

        ulong targetSteamId = Utils.Steam322SteamId(ulong.Parse(match.Groups[1].Value));
        string tradeToken = match.Groups[2].Value;

        if (!new SteamID(targetSteamId).IsIndividualAccount)
        {
            return bot.FormatBotResponse(Langs.SteamIdInvalid);
        }

        var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
        if (inventory == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }

        var bundle = await Handler.GetAppCardBundle(bot, appId, inventory).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (bundle.Assets != null)
        {
            sb.AppendLine(Langs.InventoryStatusBeforeTrade);
            sb.AppendLine(
                string.Format(Langs.CurrentCardInventoryShow,
                appId, bundle.Assets.Count(), bundle.CardCountPerSet,
                bundle.TotalSetCount, bundle.ExtraTotalCount,
                bundle.TradableSetCount, bundle.ExtraTradableCount)
            );

            if (bundle.TradableSetCount < setCount)
            {
                sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            }
            else
            {
                var offer = new List<Asset>();
                var flag = Utils.DistinctList(bundle.Assets, x => x.ClassID).ToDictionary(x => x, _ => setCount);

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
                    sb.AppendLine(string.Format(Langs.ExpectToSendCardInfo, setCount, setCount * bundle.CardCountPerSet));
                    var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler.SendTradeOffer(targetSteamId, offer, null, tradeToken, false, Utils.Config.MaxItemPerTrade).ConfigureAwait(false);

                    if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
                    {
                        (bool twoFactorSuccess, _, _) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

                        sb.AppendLine(string.Format(Langs.TFAConfirmResult, twoFactorSuccess ? Langs.Success : Langs.Failure));

                    }

                    sb.AppendLine(string.Format(Langs.SendTradeResult, success ? Langs.Success : Langs.Failure));
                }
                else
                {
                    sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
                }
            }
        }
        else
        {
            if (bundle.CardCountPerSet == -1)
            {
                sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NetworkError));
            }
            else
            {
                sb.AppendLine(string.Format(Langs.TwoItem, appId, Langs.NoAvailableCards));
            }
            sb.AppendLine(Langs.SendTradeFailedAppIdInvalid);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 根据指定交易报价发送指定套数的卡牌 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strAppId"></param>
    /// <param name="strSetCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSendCardSet(string botNames, string strAppId, string strSetCount, string tradeLink, bool autoConfirm)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return Utils.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSendCardSet(bot, strAppId, strSetCount, tradeLink, autoConfirm))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }
}
