using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Security;
using CardTradeExtension.Data;
using System.Globalization;
using System.Text;

namespace CardTradeExtension.CSGO;

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

        var inventory = await Handler.FetchBotCSInventory(bot, x => x.Tradable).ConfigureAwait(false);
        if (inventory == null)
        {
            return bot.FormatBotResponse(Langs.LoadInventoryFailedNetworkError);
        }

        if (!inventory.Any())
        {
            return bot.FormatBotResponse(Langs.CardInventoryIsEmpty);
        }

        var classIds = inventory.Select(x => x.ClassID).Distinct();
        var orderedClassIds = classIds.Select(x => (classId: x, XmlSchemaTotalDigitsFacet: inventory.Count(y => y.ClassID == x))).OrderByDescending(z => z.XmlSchemaTotalDigitsFacet);

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
            var inventory = await Handler.FetchBotCSInventory(bot, x => x.Tradable).ConfigureAwait(false);
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
                return bot.FormatBotResponse("参数无效 SENDCSITEM [Bots] [ClassId] 发给每个Bot的数量, ClassId未指定时发送全部可交易物品, 否则只发送指定的物品");
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
    /// <param name="strPrice"></param>
    /// <param name="strCount"></param>
    /// <param name="autoConfirm"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSellCSItem(Bot bot, string strClassId, string strCount, string strPrice, bool autoConfirm)
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
            return bot.FormatBotResponse("参数无效 SELLCSITEM ClassId 数量 价格, 数量为-1时出售全部");
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
            var (success, confirmations, message) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EType.Market, null, true).ConfigureAwait(false);
            return bot.FormatBotResponse(string.Format("共计 {0} 个物品上架 {1}", inventory.Count(), success ? Langs.Success : Langs.Failure));
        }
        else
        {
            return bot.FormatBotResponse(string.Format("共计 {0} 个物品上架, 等待手动确认", inventory.Count()));
        }
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
    internal static async Task<string?> ResponseSellCSItem(string botNames, string strClassId, string strCount, string strPrice, bool autoConfirm)
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

        IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSellCSItem(bot, strClassId, strCount, strPrice, autoConfirm))).ConfigureAwait(false);

        List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }
}