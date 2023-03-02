using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

namespace CardTradeExtension.Core
{
    internal static class Command
    {
        /// <summary>
        /// 获取成套卡牌套数列表
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseGetFullSetList(Bot bot, string? extraArgs)
        {
            string? keyword = null;
            uint page = 1;
            uint num = 30;

            if (!string.IsNullOrEmpty(extraArgs))
            {
                var args = extraArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < args.Length; i += 2)
                {
                    string key = args[i];
                    string value = args[i + 1];
                    if (key != null)
                    {
                        switch (key)
                        {
                            case "-k":
                            case "-key":
                                keyword = value;
                                break;
                            case "-p":
                            case "-page":
                                if (uint.TryParse(value, out uint p))
                                {
                                    page = p;
                                }
                                break;
                            case "-n":
                            case "-num":
                                if (uint.TryParse(value, out uint n))
                                {
                                    num = n;
                                }
                                break;
                        }
                    }
                }
            }
            return await Handler.ResponseGetFullSetList(bot, keyword, page, num).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取成套卡牌套数 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseGetFullSetList(string botNames, string? extraArgs)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGetFullSetList(bot, extraArgs))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }

        /// <summary>
        /// 获取成套卡牌套数列表
        /// </summary>
        /// <param name="bot"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseGetCardSetCountOfGame(Bot bot, string appIds)
        {
            string? keyword = null;
            uint page = 1;
            uint num = 30;

            if (!string.IsNullOrEmpty(appIds))
            {
                var args = appIds.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < args.Length; i += 2)
                {
                    string key = args[i];
                    string value = args[i + 1];
                    if (key != null)
                    {
                        switch (key)
                        {
                            case "-k":
                            case "-key":
                                keyword = value;
                                break;
                            case "-p":
                            case "-page":
                                if (uint.TryParse(value, out uint p))
                                {
                                    page = p;
                                }
                                break;
                            case "-n":
                            case "-num":
                                if (uint.TryParse(value, out uint n))
                                {
                                    num = n;
                                }
                                break;
                        }
                    }
                }
            }
            return await Handler.ResponseGetSetCountOfGame(bot, keyword, page, num).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取成套卡牌套数 (多个Bot)
        /// </summary>
        /// <param name="botNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static async Task<string?> ResponseGetCardSetCountOfGame(string botNames, string appIds)
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

            IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGetCardSetCountOfGame(bot, appIds))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);

            return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
        }


    }
}
