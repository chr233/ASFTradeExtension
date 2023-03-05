using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
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
        internal static async Task<string?> ResponseGetCardSetCountOfGame(Bot bot, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            List<uint> appIds = new();

            var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (!queries.Any())
            {
                return bot.FormatBotResponse("输入的游戏ID无效");
            }

            foreach (var q in queries)
            {
                if (uint.TryParse(q, out uint appId))
                {
                    appIds.Add(appId);
                }
            }

            if (appIds.Any())
            {
                var inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
                IList<int> results = await Utilities.InParallel(appIds.Select(appId => CacheHelper.GetCacheCardSetCount(bot, appId))).ConfigureAwait(false);

            }

            StringBuilder sb = new();

            return null;
        }

        /// <summary>
        /// 获取成套卡牌套数 (多个Bot)
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
