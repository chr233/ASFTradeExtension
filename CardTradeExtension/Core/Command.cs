using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using System.Linq;
using System.Text;

namespace CardTradeExtension.Core
{
    internal static class Command
    {
        /// <summary>
        /// 获取成套卡牌套数列表
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseCardSetList(Bot bot)
        {



            return null;
        }

        /// <summary>
        /// 获取成套卡牌套数 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseCardSetList(string botNames)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseCardSetList(bot))).ConfigureAwait(false);

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

            List<uint> appIds = new();
            foreach (var q in queries)
            {
                if (uint.TryParse(q, out uint appId))
                {
                    appIds.Add(appId);
                }
                else
                {
                    appIds.Add(0);
                }
            }

            StringBuilder sb = new();
            sb.AppendLine(Langs.MultipleLineResult);

            if (appIds.Any())
            {
                var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
                if (inventory == null)
                {
                    return bot.FormatBotResponse("网络异常, 读取库存信息失败");
                }

                var cardGroup = await Handler.GetAppCardGroup(bot, appIds, inventory).ConfigureAwait(false);

                for (int i = 0; i < appIds.Count; i++)
                {
                    uint appId = appIds[i];

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
                                    string.Format("{0}: 总计 {1} 张, 每套 {2} 张, 全部 {3}+{4} 可交易 {5}+{6} 套数+多余张数",
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

    }
}
