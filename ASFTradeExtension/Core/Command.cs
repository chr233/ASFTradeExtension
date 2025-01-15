using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data.Core;
using SteamKit2;
using System.Collections.Concurrent;
using System.Text;

namespace ASFTradeExtension.Core;

internal static class Command
{
    internal static ConcurrentDictionary<Bot, InventoryHandler> Handlers { get; } = new();

    /// <summary>
    /// 获取成套卡牌套数列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseFullSetList(Bot bot, string? query, bool foilCard)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var page = 0;
        var count = 20;

        if (!string.IsNullOrEmpty(query))
        {
            var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (queries.Length % 2 != 0)
                return bot.FormatBotResponse(Langs.ArgumentInvalidFSL);

            for (var i = 0; i < queries.Length; i += 2)
            {
                var option = queries[i].ToLowerInvariant();
                var value = queries[i + 1];

                switch (option)
                {
                    case "-p":
                    case "-page":
                        if (int.TryParse(value, out var p) && p > 0)
                            page = p;

                        continue;
                    case "-l":
                    case "-line":
                        if (int.TryParse(value, out var l) && l > 0)
                            count = l;

                        continue;
                }
            }
        }

        var inventoryBundles = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (inventoryBundles.Count == 0)
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);

        var invBundles = inventoryBundles
            .OrderByDescending(static kv => kv.Value.Assets?.Count ?? 0)
            .Skip(page * count)
            .Take(count);

        List<AssetBundle> bundles = [];
        foreach (var (appId, bundle) in invBundles)
            if (appId != SaleEventAppId)
                bundles.Add(bundle);

        if (bundles.Count == 0)
            return bot.FormatBotResponse(Langs.NoAvilableItemToShow);

        await handler.LoadAppCardGroup(bundles).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(foilCard ? Langs.FoilCardInventory : Langs.CardInventory);

        foreach (var bundle in bundles)
            if (bundle.Assets != null)
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    bundle.AppId, bundle.Assets.Count, bundle.CardCountPerSet,
                    bundle.TradableSetCount, bundle.ExtraTradableCount,
                    bundle.NonTradableSetCount, bundle.ExtraNonTradableCount
                );
            else
                if (bundle.CardCountPerSet == -1)
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NetworkError);
            else
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NoAvilableCards);

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取成套卡牌套数 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseFullSetList(string botNames, string? query, bool foilCard)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities.InParallel(bots.Select(bot => ResponseFullSetList(bot, query, foilCard)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取促销卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseFullSetListSaleEvent(Bot bot)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var inventoryBundles = await handler.GetCardSetCache(false).ConfigureAwait(false);
        var foilInventoryBundles = await handler.GetFoilCardSetCache(false).ConfigureAwait(false);

        var bundle = inventoryBundles.GetValueOrDefault(SaleEventAppId);

        var foilBundle = foilInventoryBundles.GetValueOrDefault(SaleEventAppId);

        await handler.LoadEventAppCardGroup(bundle, foilBundle).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(Langs.SaleEventCardInventory);

        if (bundle != null)
            if (bundle.Assets != null)
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    bundle.AppId, bundle.Assets.Count, bundle.CardCountPerSet,
                    bundle.TradableSetCount, bundle.ExtraTradableCount,
                    bundle.NonTradableSetCount, bundle.ExtraNonTradableCount
                );
            else
                if (bundle.CardCountPerSet == -1)
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NetworkError);
            else
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NoAvilableCards);
        else
            sb.AppendLine(Langs.NoInventory);

        sb.AppendLine(Langs.FoilSaleEventCardInventory);

        if (foilBundle != null)
            if (foilBundle.Assets != null)
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    foilBundle.AppId, foilBundle.Assets.Count, foilBundle.CardCountPerSet,
                    foilBundle.TradableSetCount, foilBundle.ExtraTradableCount,
                    foilBundle.NonTradableSetCount, foilBundle.ExtraNonTradableCount
                );
            else
                if (foilBundle.CardCountPerSet == -1)
                sb.AppendLineFormat(Langs.TwoItem, foilBundle.AppId, Langs.NetworkError);
            else
                sb.AppendLineFormat(Langs.TwoItem, foilBundle.AppId, Langs.NoAvilableCards);
        else
            sb.AppendLine(Langs.NoInventory);

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取促销卡牌套数 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseFullSetListSaleEvent(string botNames)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities.InParallel(bots.Select(static bot => ResponseFullSetListSaleEvent(bot)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取宝珠信息
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGemsInfo(Bot bot)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var gemsInfo = await handler.GetGemsInfoCache(false).ConfigureAwait(false);

        ulong tradableGems = 0;
        ulong nonTradableGems = 0;
        ulong tradableBags = 0;
        ulong nonTradableBags = 0;

        foreach (var asset in gemsInfo.GemAssets)
            if (asset.Tradable)
                tradableGems += asset.Amount;
            else
                nonTradableGems += asset.Amount;

        foreach (var asset in gemsInfo.BagAssets)
            if (asset.Tradable)
                tradableBags += asset.Amount;
            else
                nonTradableBags += asset.Amount;

        var totalGems = tradableGems + nonTradableGems;
        var totalBags = tradableBags + nonTradableBags;

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLineFormat(Langs.GemsSummary, tradableGems, nonTradableGems, totalGems);
        sb.AppendLineFormat(Langs.GemBagsSunnary, tradableBags, nonTradableBags, totalBags);

        var totalTradableGems = tradableGems + tradableBags * 1000;
        var TotalNonTradableGems = nonTradableGems + nonTradableBags * 1000;
        var GemsSum = totalTradableGems + TotalNonTradableGems;
        sb.AppendLineFormat(Langs.TotalGemsSummary, totalTradableGems, TotalNonTradableGems, GemsSum);
        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取宝珠信息 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseGemsInfo(string botNames)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities.InParallel(bots.Select(static bot => ResponseGemsInfo(bot)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取指定游戏成套卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseFullSetCountOfGame(Bot bot, string query, bool foilCard)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var entries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
            return bot.FormatBotResponse(Langs.ArgumentInvalidAppIds);

        var inventory = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (inventory.Count == 0)
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(foilCard ? Langs.FoilCardInventory : Langs.CardInventory);

        var i = 0;
        foreach (var entry in entries)
            if (!uint.TryParse(entry, out var appId) || appId == 0)
                sb.AppendLineFormat(Langs.TwoItem, entries[i], Langs.AppIdInvalid);
            else
            {
                if (inventory.TryGetValue(appId, out var bundle))
                    if (bundle.Assets != null)
                        sb.AppendLineFormat(
                            Langs.CurrentCardInventoryShow,
                            appId, bundle.Assets.Count, bundle.CardCountPerSet,
                            bundle.TradableSetCount, bundle.ExtraTradableCount,
                            bundle.NonTradableSetCount, bundle.ExtraNonTradableCount
                        );
                    else
                        if (bundle.CardCountPerSet == -1)
                        sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NetworkError);
                    else
                        sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoAvilableCards);
                else
                    sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoInformation);

                i++;
            }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取指定游戏成套卡牌套数 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseFullSetCountOfGame(string botNames, string query, bool foilCard)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities.InParallel(bots.Select(bot => ResponseFullSetCountOfGame(bot, query, foilCard)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 向机器人发送指定套数的卡牌
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strAppId"></param>
    /// <param name="strSetCount"></param>
    /// <param name="botName"></param>
    /// <param name="autoConfirm"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSendCardSetBot(Bot bot, string strAppId, string strSetCount,
        string botName, bool autoConfirm, bool foilCard)
    {
        var targetBot = Bot.GetBot(botName);
        if (targetBot == null)
            return bot.FormatBotResponse(Strings.BotNotFound, botName);

        if (!Handlers.TryGetValue(targetBot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        var tradeLink = await handler.GetTradeLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(tradeLink))
            return bot.FormatBotResponse(Langs.FetchTradeLinkFailed);

        return await ResponseSendCardSet(bot, strAppId, strSetCount, tradeLink, autoConfirm, foilCard)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 向机器人发送指定套数的卡牌 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strAppId"></param>
    /// <param name="strSetCount"></param>
    /// <param name="botName"></param>
    /// <param name="autoConfirm"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSendCardSetBot(string botNames, string strAppId, string strSetCount,
        string botName, bool autoConfirm, bool foilCard)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities
            .InParallel(bots.Select(bot =>
                ResponseSendCardSetBot(bot, strAppId, strSetCount, botName, autoConfirm, foilCard)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

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
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSendCardSet(Bot bot, string strAppId, string strSetCount,
        string tradeLink, bool autoConfirm, bool foilCard)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var match = RegexUtils.MatchTradeLink().Match(tradeLink);

        if (!uint.TryParse(strAppId, out var appId) || !uint.TryParse(strSetCount, out var setCount) || !match.Success)
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS);

        if (appId == 0 || setCount == 0)
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);

        var targetSteamId = Steam322SteamId(ulong.Parse(match.Groups[1].Value));
        var tradeToken = match.Groups[2].Value;

        if (!new SteamID(targetSteamId).IsIndividualAccount)
            return bot.FormatBotResponse(Langs.SteamIdInvalid);

        var inventory = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (!inventory.TryGetValue(appId, out var bundle))
            return bot.FormatBotResponse(Langs.TwoItem, appId, Langs.NoAvilableCards);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (bundle.Assets != null)
        {
            sb.AppendLine(Langs.InventoryStatusBeforeTrade);
            sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                appId, bundle.Assets.Count, bundle.CardCountPerSet,
                bundle.TradableSetCount, bundle.ExtraTradableCount,
                bundle.NonTradableSetCount, bundle.ExtraNonTradableCount
            );

            if (bundle.TradableSetCount < setCount)
                sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            else
            {
                var offer = new List<Asset>();
                var flag = bundle.Assets.Select(static x => x.ClassID).Distinct()
                    .ToDictionary(static x => x, _ => setCount);

                foreach (var asset in bundle.Assets)
                {
                    var clsId = asset.ClassID;
                    if (flag[clsId] > 0)
                    {
                        offer.Add(asset);
                        flag[clsId]--;
                    }
                }

                if (offer.Count != 0)
                {
                    await handler.AddInTradeItems(offer).ConfigureAwait(false);

                    sb.AppendLineFormat(Langs.ExpectToSendCardInfo, setCount, setCount * bundle.CardCountPerSet);
                    var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                        .SendTradeOffer(targetSteamId, offer, null, tradeToken, null, false, Config.MaxItemPerTrade)
                        .ConfigureAwait(false);

                    if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
                    {
                        var (twoFactorSuccess, _, _) = await bot.Actions
                            .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                                mobileTradeOfferIDs, true).ConfigureAwait(false);

                        sb.AppendLineFormat(Langs.TFAConfirmResult, twoFactorSuccess ? Langs.Success : Langs.Failure);
                    }

                    sb.AppendLineFormat(Langs.SendTradeResult, success ? Langs.Success : Langs.Failure);
                }
                else
                    sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            }
        }
        else
        {
            if (bundle.CardCountPerSet == -1)
                sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NetworkError);
            else
                sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoAvilableCards);

            sb.AppendLine(Langs.SendTradeFailedAppIdInvalid);
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 根据指定交易报价发送指定套数的卡牌 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strAppId"></param>
    /// <param name="strSetCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSendCardSet(string botNames, string strAppId, string strSetCount,
        string tradeLink, bool autoConfirm, bool foilCard)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities
            .InParallel(bots.Select(bot =>
                ResponseSendCardSet(bot, strAppId, strSetCount, tradeLink, autoConfirm, foilCard)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 向机器人发送指定数量的宝珠
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strGemCount"></param>
    /// <param name="botName"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSendGemsBot(Bot bot, string? strGemCount, string botName,
        bool autoConfirm)
    {
        var targetBot = Bot.GetBot(botName);
        if (targetBot == null)
            return bot.FormatBotResponse(Strings.BotNotFound, botName);

        if (!Handlers.TryGetValue(targetBot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        var tradeLink = await handler.GetTradeLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(tradeLink))
            return bot.FormatBotResponse(Langs.FetchTradeLinkFailed);

        return await ResponseSendGems(bot, strGemCount, tradeLink, autoConfirm).ConfigureAwait(false);
    }

    /// <summary>
    /// 向机器人发送指定数量的宝珠 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strGemCount"></param>
    /// <param name="botName"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSendGemsBot(string botNames, string? strGemCount, string botName,
        bool autoConfirm)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities
            .InParallel(bots.Select(bot => ResponseSendGemsBot(bot, strGemCount, botName, autoConfirm)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 根据指定交易报价发送指定数量的宝珠
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strGemCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSendGems(Bot bot, string? strGemCount, string tradeLink,
        bool autoConfirm)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        var match = RegexUtils.MatchTradeLink().Match(tradeLink);

        if (!ulong.TryParse(strGemCount, out var gemCount))
            gemCount = 0;

        if (gemCount == 0 || !match.Success)
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);

        var targetSteamId = Steam322SteamId(ulong.Parse(match.Groups[1].Value));
        var tradeToken = match.Groups[2].Value;

        if (!new SteamID(targetSteamId).IsIndividualAccount)
            return bot.FormatBotResponse(Langs.SteamIdInvalid);

        var gemsInfo = await handler.GetGemsInfoCache(false).ConfigureAwait(false);

        ulong offerGems = 0;
        List<Asset> offers = [];

        List<Asset> newBagsList = [];
        ulong offerBagCount = 0;
        foreach (var asset in gemsInfo.BagAssets)
        {
            var needBag = (gemCount - offerGems) / 1000;
            if (needBag == 0)
                break;

            Asset offerAsset;
            if (asset.Amount > needBag) //有剩余宝珠, 拆分以后记录新Asset
            {
                offerAsset = asset.CopyWithAmount(needBag);

                var newAsset = asset.CopyWithAmount(asset.Amount - needBag);
                newBagsList.Add(newAsset);
            }
            else //无剩余宝珠, 直接添加到 offer
                offerAsset = asset;

            offers.Add(offerAsset);
            offerGems += offerAsset.Amount * 1000;
            offerBagCount += offerAsset.Amount;
        }

        gemsInfo.BagAssets = newBagsList;

        List<Asset> newGemsList = [];
        ulong offerGemsCount = 0;
        foreach (var asset in gemsInfo.GemAssets)
        {
            var needGem = gemCount - offerGems;
            if (needGem == 0)
                break;

            Asset offerAsset;
            if (asset.Amount > needGem) //有剩余宝珠, 拆分以后记录新Asset
            {
                offerAsset = asset.CopyWithAmount(needGem);

                var newAsset = asset.CopyWithAmount(asset.Amount - needGem);
                newGemsList.Add(newAsset);
            }
            else //无剩余宝珠, 直接添加到 offer
                offerAsset = asset;

            offers.Add(offerAsset);
            offerGems += offerAsset.Amount;
            offerGemsCount += offerAsset.Amount;
        }

        gemsInfo.GemAssets = newGemsList;

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (offerGems < gemCount || offers.Count == 0)
        {
            var offerSum = offerBagCount * 1000 + offerGemsCount;
            sb.AppendLineFormat(Langs.SendTradeFailedGemsNotEnough);
            sb.AppendLineFormat(Langs.TradableGemsSummary, offerBagCount, offerGemsCount, offerSum);
            sb.AppendLineFormat(Langs.LackOfGems, gemCount, gemCount - offerSum);
        }
        else
        {
            sb.AppendLineFormat(Langs.GemTradeSummary, offerBagCount, offerGemsCount, gemCount);
            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, offers, null, tradeToken, null, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);

                sb.AppendLineFormat(Langs.TFAConfirmResult, twoFactorSuccess ? Langs.Success : Langs.Failure);
            }

            sb.AppendLineFormat(Langs.SendTradeResult, success ? Langs.Success : Langs.Failure);
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 根据指定交易报价发送指定数量的宝珠 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strGemCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSendGems(string botNames, string? strGemCount, string tradeLink,
        bool autoConfirm)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities
            .InParallel(bots.Select(bot => ResponseSendGems(bot, strGemCount, tradeLink, autoConfirm)))
            .ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 刷新库存缓存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseReloadCache(Bot bot)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
            return bot.FormatBotResponse(Langs.InternalError);

        if (!bot.IsConnectedAndLoggedOn)
            return bot.FormatBotResponse(Strings.BotNotConnected);

        handler.ExpiredCache();
        await handler.GetBotInventory(true).ConfigureAwait(false);

        return bot.FormatBotResponse(Langs.ReloadInventoryCacheDone);
    }

    /// <summary>
    /// 刷新库存缓存 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseReloadCache(string botNames)
    {
        if (string.IsNullOrEmpty(botNames))
            throw new ArgumentNullException(nameof(botNames));

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
            return FormatStaticResponse(Strings.BotNotFound, botNames);

        var results = await Utilities.InParallel(bots.Select(bot => ResponseReloadCache(bot))).ConfigureAwait(false);
        var responses = new List<string>(results.Where(static result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }
}