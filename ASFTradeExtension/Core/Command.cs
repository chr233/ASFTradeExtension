using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data.Core;
using SteamKit2;
using System.Text;

namespace ASFTradeExtension.Core;

internal static class Command
{
    /// <summary>
    /// 获取发货机器人
    /// </summary>
    /// <returns></returns>
    public static string ResponseGetMasterBot()
    {
        if (string.IsNullOrEmpty(CardSetCache.MasterBotName))
        {
            return FormatStaticResponse(Langs.MasterBotNotSet);
        }

        return FormatStaticResponse($"当前库存机器人为 -> {CardSetCache.MasterBotName}");
    }

    /// <summary>
    /// 设置发货机器人
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    public static async Task<string> ResponseSetMasterBot(Bot bot)
    {
        //if (!bot.HasMobileAuthenticator)
        //{
        //    return FormatStaticResponse("设置失败, 发货机器人需导入手机令牌");
        //}

        await CardSetCache.SetMasterBotName(bot.BotName).ConfigureAwait(false);
        await CardSetCache.SaveCacheFile().ConfigureAwait(false);
        return FormatStaticResponse($"设置发货机器人为 -> {CardSetCache.MasterBotName}");
    }

    /// <summary>
    /// 设置发货机器人
    /// </summary>
    /// <param name="botName"></param>
    /// <returns></returns>
    public static Task<string> ResponseSetMasterBot(string botName)
    {
        var bot = Bot.GetBot(botName);
        if (bot == null)
        {
            return Task.FromResult("设置失败, 未找到机器人");
        }

        return ResponseSetMasterBot(bot);
    }

    /// <summary>
    /// 获取成套卡牌套数列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseFullSetList(string? query, bool foilCard)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var page = 0;
        var count = 20;

        if (!string.IsNullOrEmpty(query))
        {
            var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (queries.Length % 2 != 0)
            {
                return bot.FormatBotResponse(Langs.ArgumentInvalidFSL);
            }

            for (var i = 0; i < queries.Length; i += 2)
            {
                var option = queries[i].ToLowerInvariant();
                var value = queries[i + 1];

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

        var inventoryBundles = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (inventoryBundles.Count == 0)
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }

        var invBundles = inventoryBundles
            .OrderByDescending(static kv => kv.Value.Assets.Count)
            .Skip(page * count)
            .Take(count);

        List<AssetBundle> bundles = [];
        foreach (var (appId, bundle) in invBundles)
        {
            if (appId != SaleEventAppId)
            {
                bundles.Add(bundle);
            }
        }

        if (bundles.Count == 0)
        {
            return bot.FormatBotResponse(Langs.NoAvilableItemToShow);
        }

        await handler.FullLoadAppCardGroup(bundles).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(foilCard ? Langs.FoilCardInventory : Langs.CardInventory);

        foreach (var bundle in bundles)
        {
            if (bundle.Assets != null)
            {
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    bundle.AppId, bundle.Assets.Count, bundle.CardCountPerSet,
                    bundle.TradableSetCount, bundle.ExtraTradableCount,
                    bundle.ExtraNonTradableCount
                );
            }
            else if (bundle.CardCountPerSet == -1)
            {
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NetworkError);
            }
            else
            {
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NoAvilableCards);
            }
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取促销卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseFullSetListSaleEvent()
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var inventoryBundles = await handler.GetCardSetCache(false).ConfigureAwait(false);
        var foilInventoryBundles = await handler.GetFoilCardSetCache(false).ConfigureAwait(false);

        var bundle = inventoryBundles.GetValueOrDefault(SaleEventAppId);

        var foilBundle = foilInventoryBundles.GetValueOrDefault(SaleEventAppId);

        await handler.LoadEventAppCardGroup(bundle, foilBundle).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(Langs.SaleEventCardInventory);

        if (bundle != null)
        {
            if (bundle.Assets != null)
            {
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    bundle.AppId, bundle.Assets.Count, bundle.CardCountPerSet,
                    bundle.TradableSetCount, bundle.ExtraTradableCount,
                    bundle.ExtraNonTradableCount
                );
            }
            else if (bundle.CardCountPerSet == -1)
            {
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NetworkError);
            }
            else
            {
                sb.AppendLineFormat(Langs.TwoItem, bundle.AppId, Langs.NoAvilableCards);
            }
        }
        else
        {
            sb.AppendLine(Langs.NoInventory);
        }

        sb.AppendLine(Langs.FoilSaleEventCardInventory);

        if (foilBundle != null)
        {
            if (foilBundle.Assets != null)
            {
                sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                    foilBundle.AppId, foilBundle.Assets.Count, foilBundle.CardCountPerSet,
                    foilBundle.TradableSetCount, foilBundle.ExtraTradableCount,
                    foilBundle.ExtraNonTradableCount
                );
            }
            else if (foilBundle.CardCountPerSet == -1)
            {
                sb.AppendLineFormat(Langs.TwoItem, foilBundle.AppId, Langs.NetworkError);
            }
            else
            {
                sb.AppendLineFormat(Langs.TwoItem, foilBundle.AppId, Langs.NoAvilableCards);
            }
        }
        else
        {
            sb.AppendLine(Langs.NoInventory);
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取宝珠信息
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseGemsInfo()
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var gemsInfo = await handler.GetGemsInfoCache(false).ConfigureAwait(false);

        ulong tradableGems = 0;
        ulong nonTradableGems = 0;
        ulong tradableBags = 0;
        ulong nonTradableBags = 0;

        foreach (var asset in gemsInfo.GemAssets)
        {
            if (asset.Tradable)
            {
                tradableGems += asset.Amount;
            }
            else
            {
                nonTradableGems += asset.Amount;
            }
        }

        foreach (var asset in gemsInfo.BagAssets)
        {
            if (asset.Tradable)
            {
                tradableBags += asset.Amount;
            }
            else
            {
                nonTradableBags += asset.Amount;
            }
        }

        var totalGems = tradableGems + nonTradableGems;
        var totalBags = tradableBags + nonTradableBags;

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLineFormat(Langs.GemsSummary, tradableGems, nonTradableGems, totalGems);
        sb.AppendLineFormat(Langs.GemBagsSunnary, tradableBags, nonTradableBags, totalBags);

        var totalTradableGems = tradableGems + (tradableBags * 1000);
        var TotalNonTradableGems = nonTradableGems + (nonTradableBags * 1000);
        var GemsSum = totalTradableGems + TotalNonTradableGems;
        sb.AppendLineFormat(Langs.TotalGemsSummary, totalTradableGems, TotalNonTradableGems, GemsSum);
        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 获取指定游戏成套卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="query"></param>
    /// <param name="foilCard"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseFullSetCountOfGame(string query, bool foilCard)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var entries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidAppIds);
        }

        var inventory = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (inventory.Count == 0)
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);
        sb.AppendLine(foilCard ? Langs.FoilCardInventory : Langs.CardInventory);

        var i = 0;
        foreach (var entry in entries)
        {
            if (!uint.TryParse(entry, out var appId) || appId == 0)
            {
                sb.AppendLineFormat(Langs.TwoItem, entries[i], Langs.AppIdInvalid);
            }
            else
            {
                if (inventory.TryGetValue(appId, out var bundle))
                {
                    if (bundle.Assets != null)
                    {
                        sb.AppendLineFormat(
                            Langs.CurrentCardInventoryShow,
                            appId, bundle.Assets.Count, bundle.CardCountPerSet,
                            bundle.TradableSetCount, bundle.ExtraTradableCount,
                            bundle.ExtraNonTradableCount
                        );
                    }
                    else if (bundle.CardCountPerSet == -1)
                    {
                        sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NetworkError);
                    }
                    else
                    {
                        sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoAvilableCards);
                    }
                }
                else
                {
                    sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoInformation);
                }

                i++;
            }
        }

        return bot.FormatBotResponse(sb.ToString());
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
    internal static async Task<string> ResponseSendCardSetBot(string strAppId, string strSetCount,
        string botName, bool autoConfirm, bool foilCard)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var targetBot = Bot.GetBot(botName);
        if (targetBot == null || !CoreHandlers.TryGetValue(targetBot, out var targetHandler))
        {
            return bot.FormatBotResponse(Strings.BotNotFound, botName);
        }

        var tradeLink = await targetHandler.GetTradeLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(tradeLink))
        {
            return bot.FormatBotResponse(Langs.FetchTradeLinkFailed);
        }

        return await ResponseSendCardSet(strAppId, strSetCount, tradeLink, autoConfirm, foilCard)
            .ConfigureAwait(false);
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
    internal static async Task<string> ResponseSendCardSet(string strAppId, string strSetCount,
        string tradeLink, bool autoConfirm, bool foilCard)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var (valid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!valid || string.IsNullOrEmpty(tradeToken))
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS);
        }

        if (!uint.TryParse(strAppId, out var appId) || !uint.TryParse(strSetCount, out var setCount) || appId == 0 || setCount == 0)
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);
        }

        if (!new SteamID(targetSteamId).IsIndividualAccount)
        {
            return bot.FormatBotResponse(Langs.SteamIdInvalid);
        }

        var inventory = await (
            foilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        if (!inventory.TryGetValue(appId, out var bundle))
        {
            return bot.FormatBotResponse(Langs.TwoItem, appId, Langs.NoAvilableCards);
        }

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (bundle.Assets != null)
        {
            sb.AppendLine(Langs.InventoryStatusBeforeTrade);
            sb.AppendLineFormat(Langs.CurrentCardInventoryShow,
                appId, bundle.Assets.Count, bundle.CardCountPerSet,
                bundle.TradableSetCount, bundle.ExtraTradableCount,
                bundle.ExtraNonTradableCount
            );

            if (bundle.TradableSetCount < setCount)
            {
                sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
            }
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
                {
                    sb.AppendLine(Langs.SendTradeFailedNoEnoughCards);
                }
            }
        }
        else
        {
            if (bundle.CardCountPerSet == -1)
            {
                sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NetworkError);
            }
            else
            {
                sb.AppendLineFormat(Langs.TwoItem, appId, Langs.NoAvilableCards);
            }

            sb.AppendLine(Langs.SendTradeFailedAppIdInvalid);
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 向机器人发送指定数量的宝珠
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strGemCount"></param>
    /// <param name="botName"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseSendGemsBot(string? strGemCount, string botName,
        bool autoConfirm)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var targetBot = Bot.GetBot(botName);
        if (targetBot == null || !CoreHandlers.TryGetValue(targetBot, out var targetHandler))
        {
            return bot.FormatBotResponse(Strings.BotNotFound, botName);
        }

        var tradeLink = await targetHandler.GetTradeLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(tradeLink))
        {
            return bot.FormatBotResponse(Langs.FetchTradeLinkFailed);
        }

        return await ResponseSendGems(strGemCount, tradeLink, autoConfirm).ConfigureAwait(false);
    }

    /// <summary>
    /// 根据指定交易报价发送指定数量的宝珠
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strGemCount"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseSendGems(string? strGemCount, string tradeLink,
        bool autoConfirm)
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        var (valid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!valid || string.IsNullOrEmpty(tradeToken))
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS);
        }

        if (!uint.TryParse(strGemCount, out var gemCount) || gemCount == 0)
        {
            return bot.FormatBotResponse(Langs.ArgumentInvalidSCS2);
        }

        if (!new SteamID(targetSteamId).IsIndividualAccount)
        {
            return bot.FormatBotResponse(Langs.SteamIdInvalid);
        }

        var gemsInfo = await handler.GetGemsInfoCache(false).ConfigureAwait(false);

        ulong offerGems = 0;
        List<Asset> offers = [];

        List<Asset> newBagsList = [];
        ulong offerBagCount = 0;
        foreach (var asset in gemsInfo.BagAssets)
        {
            var needBag = (gemCount - offerGems) / 1000;
            if (needBag == 0)
            {
                break;
            }

            Asset offerAsset;
            if (asset.Amount > needBag) //有剩余宝珠, 拆分以后记录新Asset
            {
                offerAsset = asset.CopyWithAmount(needBag);

                var newAsset = asset.CopyWithAmount(asset.Amount - needBag);
                newBagsList.Add(newAsset);
            }
            else //无剩余宝珠, 直接添加到 offer
            {
                offerAsset = asset;
            }

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
            {
                break;
            }

            Asset offerAsset;
            if (asset.Amount > needGem) //有剩余宝珠, 拆分以后记录新Asset
            {
                offerAsset = asset.CopyWithAmount(needGem);

                var newAsset = asset.CopyWithAmount(asset.Amount - needGem);
                newGemsList.Add(newAsset);
            }
            else //无剩余宝珠, 直接添加到 offer
            {
                offerAsset = asset;
            }

            offers.Add(offerAsset);
            offerGems += offerAsset.Amount;
            offerGemsCount += offerAsset.Amount;
        }

        gemsInfo.GemAssets = newGemsList;

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        if (offerGems < gemCount || offers.Count == 0)
        {
            var offerSum = (offerBagCount * 1000) + offerGemsCount;
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
    /// 刷新库存缓存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string> ResponseReloadCache()
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        handler.ExpiredCache();
        await handler.GetBotInventory(true).ConfigureAwait(false);

        return bot.FormatBotResponse(Langs.ReloadInventoryCacheDone);
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    /// <returns></returns>
    internal static async Task<string> ResponseClearCache()
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        handler.ExpiredCache();
        await handler.ClearIntradeItems().ConfigureAwait(false);

        return bot.FormatBotResponse("已清除交易中物品缓存");
    }

    /// <summary>
    /// 获取机器人库存
    /// </summary>
    /// <returns></returns>
    public static async Task<string> ResponseGetBotStock()
    {
        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        try
        {
            var invCache = await handler.GetCardSetCache(false).ConfigureAwait(false);
            await handler.FullLoadAppCardGroup(invCache).ConfigureAwait(false);

            if (invCache.Count == 0)
            {
                return FormatStaticResponse("机器人库存为空");
            }

            var sortedInv = invCache
                .Select(static x => x.Value)
                .OrderByDescending(static x => x.TradableSetCount);

            var sb = new StringBuilder();

            var totalSet = 0;
            var totalCard = 0;
            var notLoaded = 0;

            sb.AppendLine("机器人库存:");
            foreach (var inv in sortedInv)
            {
                if (inv.Loaded)
                {
                    sb.AppendLine(
                        $"AppID: {inv.AppId}, 可交易卡牌 {inv.TradableSetCount} 套 + {inv.ExtraTradableCount} 张, 每套 {inv.CardCountPerSet} 张");
                    totalSet += inv.TradableSetCount;
                    totalCard += inv.TradableSetCount * inv.CardCountPerSet;
                }
                else
                {
                    sb.AppendLine($"AppID: {inv.AppId}, 套数信息未加载完成");
                    notLoaded++;
                }
            }

            return FormatStaticResponse($"库存机器人 {bot.BotName} 共有 {0} 套卡牌, {0} 张卡牌");
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("获取库存失败");
            ASFLogger.LogGenericException(ex);
            return FormatStaticResponse("获取库存失败");
        }
    }

    /// <summary>
    /// 发货交易报价
    /// </summary>
    /// <param name="strLevel"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    public static async Task<string> ResponseSendLevelUpTrade(string strLevel, string tradeLink, bool autoConfirm)
    {
        if (!int.TryParse(strLevel, out var targetLevel) || targetLevel <= 0)
        {
            return FormatStaticResponse("参数错误, 用法 SENDLEVELUP 目标等级 交易链接");
        }

        if (targetLevel < 1 || targetLevel > 1000)
        {
            return FormatStaticResponse("目标等级无效, 有效范围 1~1000, 用法 SENDLEVELUP 目标等级 交易链接");
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!tradeLinkValid || targetSteamId == 0 || string.IsNullOrEmpty(tradeToken))
        {
            return FormatStaticResponse("交易链接无效, 用法 SENDLEVELUP 目标等级 交易链接");
        }

        var (bot, handler) = GetRandomBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        try
        {
            var userInfo = await handler.GetUserBasicInfo(targetSteamId).ConfigureAwait(false);
            if (userInfo == null || !userInfo.IsValid)
            {
                return FormatStaticResponse($"交易链接似乎无效, 找不到用户 {targetSteamId}");
            }

            var badgeInfo = await handler.GetUserBadgeSummary(targetSteamId).ConfigureAwait(false);
            if (badgeInfo == null)
            {
                return FormatStaticResponse($"读取用户 {targetSteamId} 的徽章信息失败");
            }

            if (targetLevel <= badgeInfo.Level)
            {
                return FormatStaticResponse($"用户 {badgeInfo.Nickname}({targetSteamId}) 的等级 {badgeInfo.Level} 大于等于目标等级");
            }

            var needExp = CalcExpToLevel(badgeInfo.Level, targetLevel, badgeInfo.Experience);
            var needSet = (needExp / 100) + 1;

            var avilableSets = await handler.SelectFullSetCards(badgeInfo.Badges, needSet)
                .ConfigureAwait(false);

            var offer = avilableSets.TradeItems;
            if (needSet != avilableSets.CardSet || offer.Count == 0)
            {
                return FormatStaticResponse($"发货失败, 可用卡牌库存不足, 需要 {needSet} 套, 可发货 {avilableSets.CardSet} 套");
            }

            await handler.AddInTradeItems(avilableSets.TradeItems).ConfigureAwait(false);

            var tradeMsg = $"共发货 {needSet} 套 {offer.Count} 张卡牌, 可以从 {badgeInfo.Level} 级升到 {targetLevel} 级";

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, offer, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return FormatStaticResponse("发货失败, 发送报价失败, 可能网络问题");
            }

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);

                if (!twoFactorSuccess)
                {
                    return FormatStaticResponse("发货失败, 自动确认交易失败");
                }

                return FormatStaticResponse(
                    $"发送报价给 {badgeInfo.Nickname}({targetSteamId}) 成功, 从 {badgeInfo.Level} 级升级到 {targetLevel} 级还需要 {needExp} 点经验, 需要合成 {needSet} 套卡牌, 总计发送了 {avilableSets.CardSet} 套 {avilableSets.CardCount} 张卡牌");
            }

            return FormatStaticResponse(
                $"发送报价给 {badgeInfo.Nickname}({targetSteamId}) 成功, 需要手动确认报价, 从 {badgeInfo.Level} 级升级到 {targetLevel} 级还需要 {needExp} 点经验, 需要合成 {needSet} 套卡牌, 总计发送了 {avilableSets.CardSet} 套 {avilableSets.CardCount} 张卡牌");
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return FormatStaticResponse("发货失败, 执行发货出错");
        }
    }

    /// <summary>
    /// 发货交易报价
    /// </summary>
    /// <param name="strLevel"></param>
    /// <param name="tradeLink"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    public static async Task<string> ResponseSendLevelUpTradeSet(string steSetCount, string tradeLink, bool autoConfirm)
    {
        if (!int.TryParse(steSetCount, out var targetSet) || targetSet <= 0)
        {
            return FormatStaticResponse("参数错误, 用法 SENDLEVELUPSET 目标套数 交易链接");
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!tradeLinkValid || targetSteamId == 0 || string.IsNullOrEmpty(tradeToken))
        {
            return FormatStaticResponse("交易链接无效, 参数错误, 用法 SENDLEVELUPSET 目标套数 交易链接");
        }

        var (bot, handler) = GetMasterBot();
        if (bot == null || handler == null)
        {
            return FormatStaticResponse("未设置发货机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("发货机器人当前离线, 请稍后再试");
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return FormatStaticResponse("发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人");
        }

        try
        {
            var userInfo = await handler.GetUserBasicInfo(targetSteamId).ConfigureAwait(false);
            if (userInfo == null || !userInfo.IsValid)
            {
                return FormatStaticResponse($"交易链接似乎无效, 找不到用户 {targetSteamId}");
            }

            var badgeInfo = await handler.GetUserBadgeSummary(targetSteamId).ConfigureAwait(false);
            if (badgeInfo == null)
            {
                return FormatStaticResponse($"读取用户 {targetSteamId} 的徽章信息失败");
            }

            var availableSets = await handler.SelectFullSetCards(badgeInfo.Badges, targetSet)
                .ConfigureAwait(false);

            var offer = availableSets.TradeItems;
            if (targetSet != availableSets.CardSet || offer.Count == 0)
            {
                return FormatStaticResponse($"发货失败, 可用卡牌库存不足, 需要 {targetSet} 套, 可发货 {availableSets.CardSet} 套");
            }

            await handler.AddInTradeItems(availableSets.TradeItems).ConfigureAwait(false);

            var tradeMsg = $"共发货 {targetSet} 套 {offer.Count} 张卡牌";

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, offer, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return FormatStaticResponse("发货失败, 发送报价失败, 可能网络问题");
            }

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);

                if (!twoFactorSuccess)
                {
                    return FormatStaticResponse("发货失败, 自动确认交易失败");
                }

                return FormatStaticResponse(
                    $"发送报价给 {badgeInfo.Nickname}({targetSteamId}) 成功, 总计发送了 {availableSets.CardSet} 套 {availableSets.CardCount} 张卡牌");
            }

            return FormatStaticResponse(
                $"发送报价给 {badgeInfo.Nickname}({targetSteamId}) 成功, 需要手动确认报价, 总计发送了 {availableSets.CardSet} 套 {availableSets.CardCount} 张卡牌");
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return FormatStaticResponse("发货失败, 执行发货出错");
        }
    }

    /// <summary>
    /// 使用交易链接转移卡牌
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strRealAppId"></param>
    /// <returns></returns>
    public static async Task<string> ResponseTransferEx(Bot bot, string strRealAppId, string tradeLink, bool autoConfirm)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("机器人当前离线, 请稍后再试");
        }

        if (!uint.TryParse(strRealAppId, out var appId) || appId == 0)
        {
            return FormatStaticResponse("参数错误, 用法 TRANSFEREX [Bot] [AppId] 交易链接");
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!tradeLinkValid || targetSteamId == 0 || string.IsNullOrEmpty(tradeToken))
        {
            return FormatStaticResponse("交易链接无效, 用法 TRANSFEREX [Bot] [AppId] 交易链接");
        }

        try
        {
            var inventory = await bot.ArchiHandler.GetMyInventoryAsync(753, 6, true).ToListAsync().ConfigureAwait(false);

            List<Asset> itemToTrade = [];

            foreach (var inv in inventory)
            {
                if (inv.RealAppID == appId)
                {
                    itemToTrade.Add(inv);
                }
            }

            if (itemToTrade.Count == 0)
            {
                return bot.FormatBotResponse("当前筛选条件下无可交易物品");
            }

            var tradeMsg = $"共发货 {itemToTrade.Count} 张卡牌";

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, itemToTrade, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return FormatStaticResponse("发货失败, 发送报价失败, 可能网络问题");
            }

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);

                if (!twoFactorSuccess)
                {
                    return FormatStaticResponse("发货失败, 自动确认交易失败");
                }

                return FormatStaticResponse(
                    $"发送报价给 {targetSteamId} 成功, 总计发送了 {itemToTrade.Count} 张卡牌");
            }

            return FormatStaticResponse(
                $"发送报价给 {targetSteamId} 成功, 需要手动确认报价, 总计发送了 {itemToTrade.Count} 张卡牌");
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return FormatStaticResponse("发货失败, 执行发货出错");
        }
    }

    public static async Task<string> ResponseTransferEx(Bot bot, string strAppId, string strContextId, string tradeLink, bool autoConfirm)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return FormatStaticResponse("机器人当前离线, 请稍后再试");
        }

        if (!uint.TryParse(strAppId, out var appId) || appId == 0 || !uint.TryParse(strContextId, out var contextId) || contextId == 0)
        {
            return FormatStaticResponse("参数错误, 用法 TRANSFEREX^ [Bot] [AppId] [ContextId] 交易链接");
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!tradeLinkValid || targetSteamId == 0 || string.IsNullOrEmpty(tradeToken))
        {
            return FormatStaticResponse("交易链接无效, 用法 TRANSFEREX^ [Bot] [AppId] [ContextId] 交易链接");
        }

        try
        {
            var itemToTrade = await bot.ArchiHandler.GetMyInventoryAsync(appId, contextId, true).ToListAsync().ConfigureAwait(false);

            if (itemToTrade.Count == 0)
            {
                return bot.FormatBotResponse("当前筛选条件下无可交易物品");
            }

            var tradeMsg = $"共发货 {itemToTrade.Count} 张卡牌";

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, itemToTrade, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return FormatStaticResponse("发货失败, 发送报价失败, 可能网络问题");
            }

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);

                if (!twoFactorSuccess)
                {
                    return FormatStaticResponse("发货失败, 自动确认交易失败");
                }

                return FormatStaticResponse(
                    $"发送报价给 {targetSteamId} 成功, 总计发送了 {itemToTrade.Count} 张卡牌");
            }

            return FormatStaticResponse(
                $"发送报价给 {targetSteamId} 成功, 需要手动确认报价, 总计发送了 {itemToTrade.Count} 张卡牌");
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return FormatStaticResponse("发货失败, 执行发货出错");
        }
    }
}
