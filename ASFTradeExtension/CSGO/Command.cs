using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data;
using SteamKit2;
using System.Globalization;
using System.Text;

namespace ASFTradeExtension.Csgo;

internal static partial class Command
{
    /// <summary>
    /// 获取CSGO库存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseCsItemList(Bot bot, string? query)
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

        var inventory = await Handler.FetchBotCSInventory(bot, null).ConfigureAwait(false);
        if (inventory == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }

        if (!inventory.Any())
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }

        var itemNames = new Dictionary<ulong, string>();
        var itemCount = new Dictionary<ulong, int>();

        foreach (var asset in inventory)
        {
            if (itemCount.TryGetValue(asset.ClassID, out int total))
            {
                itemCount[asset.ClassID] += 1;
            }
            else
            {
                string name;
                if (asset.AdditionalPropertiesReadOnly?.TryGetValue("name", out var value) ?? false)
                {
                    name = value.ToString();
                }
                else
                {
                    name = "null";
                }

                itemNames[asset.ClassID] = name;
                itemCount[asset.ClassID] = 1;
            }
        }

        var orderedClassIds = itemCount.OrderByDescending(x => x.Value).Select(x => x.Key);

        var keys = orderedClassIds.Skip(page * count).Take(count);
        if (!keys.Any())
        {
            return bot.FormatBotResponse(Langs.NoAvilableItemToShow);
        }

        var sb = new StringBuilder();
        sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

        foreach (var classId in keys)
        {
            if (itemNames.TryGetValue(classId, out var name) && itemCount.TryGetValue(classId, out var total))
            {
                sb.AppendLine(string.Format(Langs.ThreeItemWithNum, name, classId, total));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取CSGO库存 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseCsItemList(string botNames, string? query)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseCsItemList(bot, query))).ConfigureAwait(false);
        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

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

        var sb = new StringBuilder();
        sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

        if (classIds.Any())
        {
            var inventory = await Handler.FetchBotCSInventory(bot, null).ConfigureAwait(false);
            if (inventory == null)
            {
                return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
            }

            if (!inventory.Any())
            {
                return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
            }

            var itemNames = new Dictionary<ulong, string>();
            var itemCount = new Dictionary<ulong, int>();

            foreach (var asset in inventory)
            {
                if (itemCount.TryGetValue(asset.ClassID, out int total))
                {
                    itemCount[asset.ClassID] += 1;
                }
                else
                {
                    string name;
                    if (asset.AdditionalPropertiesReadOnly?.TryGetValue("name", out var value) ?? false)
                    {
                        name = value.ToString();
                    }
                    else
                    {
                        name = "null";
                    }

                    itemNames[asset.ClassID] = name;
                    itemCount[asset.ClassID] = 1;
                }
            }

            foreach (var classId in classIds)
            {
                if (itemNames.TryGetValue(classId, out var name) && itemCount.TryGetValue(classId, out var total))
                {
                    sb.AppendLine(string.Format(Langs.ThreeItemWithNum, name, classId, total));
                }
            }
        }
        else
        {
            foreach (var q in queries)
            {
                sb.AppendLine(string.Format(Langs.TwoItem, q, Langs.InvalidClassId));
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

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseCSItemCount(bot, query))).ConfigureAwait(false);
        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

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
    internal static async Task<string?> ResponseSendCsItem(Bot bot, string? strClassId, string? strCountPerBot, bool autoConfirm)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        var bots = Bot.GetBots("ASF")?.Where(x => x.IsConnectedAndLoggedOn && x != bot);
        if (bots == null || !bots.Any())
        {
            return bot.FormatBotResponse(Langs.NoBotsAvilable);
        }

        var tradeToken = await WebRequests.GetTradeToken(bot).ConfigureAwait(false);

        if (string.IsNullOrEmpty(tradeToken))
        {
            return bot.FormatBotResponse(Langs.FetchTradeLinkFailed);
        }

        if (!int.TryParse(strCountPerBot, out int countPerBot))
        {
            if (!string.IsNullOrEmpty(strCountPerBot))
            {
                return bot.FormatBotResponse(Langs.SendCsItemArgsTips);
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
                return bot.FormatBotResponse(Langs.SendCsItemArgsTips);
            }
            else
            {
                classId = 0;
            }
        }

        var inventory = await Handler.FetchBotCSInventory(bot, x => x.Tradable).ConfigureAwait(false);
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

        var sb = new StringBuilder();
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
                sb.AppendLineFormat(Langs.SendTradeSuccess, b.BotName, bot.BotName, offer.Count, success ? Langs.Success : Langs.Failure);
            }
            else
            {
                sb.AppendLineFormat(Langs.SendTradeFailedNoItemAvilable, b.BotName, bot.BotName);
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
    internal static async Task<string?> ResponseSendCsItem(string botNames, string? strClassId, string? strCountPerBot, bool autoConfirm)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSendCsItem(bot, strClassId, strCountPerBot, autoConfirm))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 批量出售物品
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strClassId"></param>
    /// <param name="strPrice"></param>
    /// <param name="strCount"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSellCsItem(Bot bot, string strClassId, string strCount, string strPrice, bool autoConfirm)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        if (!ulong.TryParse(strClassId, out ulong classId) ||
            !decimal.TryParse(strPrice, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, null, out decimal price) ||
            !int.TryParse(strCount, out int count) ||
            (count == 0 || count < -1 || price < 0 || classId == 0))
        {
            return bot.FormatBotResponse(Langs.SellCsItemArgsTips);
        }

        var inventory = await Handler.FetchBotCSInventory(bot, x => x.Marketable).ConfigureAwait(false);
        if (inventory == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }
        inventory = inventory.Where(x => x.ClassID == classId);
        if (!inventory.Any())
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }
        if (count != -1)
        {
            inventory = inventory.Take(count);
        }

        var tasks = inventory.Select(x => WebRequests.SellItem(bot, x, price)).ToList();
        var results = await Utilities.InParallel(tasks).ConfigureAwait(false);

        if (autoConfirm)
        {
            var (success, confirmations, message) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Market, null, true).ConfigureAwait(false);
            return bot.FormatBotResponse(string.Format(Langs.ItemListingSuccess, inventory.Count(), success ? Langs.Success : Langs.Failure));
        }
        else
        {
            return bot.FormatBotResponse(string.Format(Langs.ItemListSuccessWaitConfirm, inventory.Count()));
        }
    }

    /// <summary>
    /// 批量出售物品 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strClassId"></param>
    /// <param name="strPrice"></param>
    /// <param name="strCount"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseSellCsItem(string botNames, string strClassId, string strCount, string strPrice, bool autoConfirm)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSellCsItem(bot, strClassId, strCount, strPrice, autoConfirm))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取市场上架物品列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strClassId"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetCsMarketInfo(Bot bot, string? strClassId)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        ulong classId;

        if (string.IsNullOrEmpty(strClassId))
        {
            classId = 0;
        }
        else
        {
            if (!ulong.TryParse(strClassId, out classId) || classId == 0)
            {
                return bot.FormatBotResponse(Langs.CsMarketHistoryArgsTips);
            }
        }

        var response = await WebRequests.GetMarketHistory(bot, 500, 0).ConfigureAwait(false);

        if (response?.Assets == null)
        {
            return bot.FormatBotResponse(Langs.NetworkError);
        }

        if (!response.Assets.TryGetValue("730", out var layer1) || !layer1.TryGetValue("2", out var layer2))
        {
            return bot.FormatBotResponse(Langs.NoSellingCsItem);
        }

        var items = layer2.Where(kv => kv.Value.Status == 2 && kv.Value.Actions != null).Select(kv => kv.Value);
        if (classId != 0)
        {
            items = items.Where(x => x.ClassId == classId);
        }

        if (!items.Any())
        {
            return bot.FormatBotResponse(classId == 0 ? Langs.NoSellingCsItem : Langs.NoSelingCsItemInFilter);
        }

        var sb = new StringBuilder();
        sb.AppendLine(Langs.MultipleLineResult);

        var itemCount = new Dictionary<ulong, int>();

        foreach (var asset in items)
        {
            if (itemCount.TryGetValue(asset.ClassId, out int count))
            {
                itemCount[asset.ClassId] = count + 1;
            }
            else
            {
                itemCount[asset.ClassId] = 1;
            }
        }

        foreach (var asset in items)
        {
            if (itemCount.Remove(asset.ClassId, out int count))
            {
                sb.AppendLine(bot.FormatBotResponse(string.Format(Langs.ThreeItemWithNum, asset.MarketName, asset.ClassId, count)));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取市场上架物品列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strClassId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseGetCsMarketInfo(string botNames, string? strClassId)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseGetCsMarketInfo(bot, strClassId))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 市场下架物品
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="strClassId"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseCsRemoveListing(Bot bot, string? strClassId)
    {
        if (!bot.IsConnectedAndLoggedOn)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        ulong classId;

        if (string.IsNullOrEmpty(strClassId))
        {
            classId = 0;
        }
        else
        {
            if (!ulong.TryParse(strClassId, out classId) || classId == 0)
            {
                return bot.FormatBotResponse(Langs.CsDeListingItemArgsTips);
            }
        }

        var response = await WebRequests.GetMarketHistory(bot, 500, 0).ConfigureAwait(false);

        if (response?.Assets == null)
        {
            return bot.FormatBotResponse(Langs.NetworkError);
        }

        if (!response.Assets.TryGetValue("730", out var layer1) || !layer1.TryGetValue("2", out var layer2))
        {
            return bot.FormatBotResponse(Langs.NoSellingCsItem);
        }

        var items = layer2.Where(kv => kv.Value.Status == 2 && kv.Value.Actions != null).Select(kv => kv.Value);
        if (classId != 0)
        {
            items = items.Where(x => x.ClassId == classId);
        }

        if (!items.Any())
        {
            return bot.FormatBotResponse(classId == 0 ? Langs.NoSellingCsItem : Langs.NoSelingCsItemInFilter);
        }

        var match = RegexUtils.MatchCsItemId();

        var itemIds = items.Select(x => x.Actions).Where(x => x?.Count > 0).Select(x => match.Match(x!.First().Link)).Where(x => x.Success).Select(x => x.Groups[1].Value);

        var tasks = itemIds.Select(x => WebRequests.RemoveMarketListing(bot, x));

        var result = await Utilities.InParallel(tasks).ConfigureAwait(false);

        int count = result.Count(x => x);

        return bot.FormatBotResponse(string.Format(Langs.DeListingSuccess, count));
    }

    /// <summary>
    /// 市场下架物品 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="strClassId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static async Task<string?> ResponseCsRemoveListing(string botNames, string? strClassId)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(Strings.BotNotFound, botNames);
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseCsRemoveListing(bot, strClassId))).ConfigureAwait(false);

        var responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 转移CS物品
    /// </summary>
    /// <param name="botName"></param>
    /// <param name="tradeLink"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseBotStatus(string botName, string tradeLink, string? option = null)
    {
        var bot = Bot.GetBot(botName);
        if (bot == null)
        {
            return FormatStaticResponse(Strings.BotNotFound, botName);
        }

        var match = RegexUtils.MatchTradeLink().Match(tradeLink);

        ulong targetSteamId = Steam322SteamId(ulong.Parse(match.Groups[1].Value));
        string tradeToken = match.Groups[2].Value;

        if (!new SteamID(targetSteamId).IsIndividualAccount)
        {
            return bot.FormatBotResponse(Langs.SteamIdInvalid);
        }

        var itemType = option?.ToUpperInvariant() switch
        {
            "CASE" => CsgoItemType.WeaponCase,
            "WEAPON" => CsgoItemType.Weapon,
            "MUSIC" => CsgoItemType.MusicKit,
            "TOOL" => CsgoItemType.Tool,
            "COLLECT" => CsgoItemType.Collectible,
            "PLAYER" => CsgoItemType.Player,
            "OTHER" => CsgoItemType.Other,
            "ALL" => CsgoItemType.All,
            _ => CsgoItemType.WeaponCase,
        };

        if (!bot.IsConnectedAndLoggedOn)
        {
            var (succ, msg) = bot.Actions.Start();
            if (!succ)
            {
                return bot.FormatBotResponse("机器人启动失败 {0}", msg);
            }

            int i = 5;
            while (i-- > 0)
            {
                await Task.Delay(2000).ConfigureAwait(false);
                if (bot.IsConnectedAndLoggedOn)
                {
                    break;
                }
            }

            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse("机器人启动超时");
            }
        }

        var invDict = await Handler.GetCsgoInventory(bot).ConfigureAwait(false);
        if (invDict == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }

        var offer = new List<Asset>();
        foreach (var (type, items) in invDict)
        {
            if (itemType.HasFlag(type))
            {
                foreach (var item in items)
                {
                    if (item.Tradable)
                    {
                        offer.Add(item);
                    }
                }
            }
        }

        if (offer.Any())
        {
            var sb = new StringBuilder();

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler.SendTradeOffer(targetSteamId, offer, null, tradeToken, false, 255).ConfigureAwait(false);

            if (mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                (bool twoFactorSuccess, _, _) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

                sb.AppendLineFormat(Langs.TFAConfirmResult, twoFactorSuccess ? Langs.Success : Langs.Failure);

            }

            sb.AppendLine(string.Format(Langs.SendTradeResult, success ? Langs.Success : Langs.Failure));

            return sb.ToString();
        }
        else
        {
            return bot.FormatBotResponse("可交易物品列表为空, 筛选模式 {0}", itemType);
        }
    }
}
