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
        internal static async Task<string?> ResponseGetCardSetCountOfGame(Bot bot, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            IEnumerable<uint> appIds;
            IEnumerable<Asset>? inventory;

            if (!string.IsNullOrEmpty(query))
            {
                var queries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (!queries.Any())
                {
                    return bot.FormatBotResponse("输入的 AppIds 无效");
                }

                List<uint> ids = new();

                foreach (var q in queries)
                {
                    if (uint.TryParse(q, out uint appId))
                    {
                        ids.Add(appId);
                    }
                }

                appIds = ids;
            }
            else
            {
                inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);


                appIds = inventory.Select(x => x.RealAppID).Distinct();
            }

            StringBuilder sb = new();

            if (appIds.Any())
            {
                inventory = await Handler.FetchBotCards(bot).ConfigureAwait(false);
                if (inventory == null)
                {
                    return bot.FormatBotResponse(Langs.NetworkError);
                }

                var cardGroup = Handler.GetAppCardGroup(bot, appIds, inventory);


                //for(int i=0;i<)

            }
            else
            {

            }


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
