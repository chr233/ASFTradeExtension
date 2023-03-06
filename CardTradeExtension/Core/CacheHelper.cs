using ArchiSteamFarm.Steam;
using System.Collections.Concurrent;


namespace CardTradeExtension.Core
{
    internal static class CacheHelper
    {
        private static ConcurrentDictionary<uint, int> FullSetCount { get; } = new();

        /// <summary>
        /// 读取缓存的每个游戏的每套卡牌张数
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="appId"></param>
        /// <returns>-1网络错误 0无卡牌</returns>
        public static async Task<int> GetCacheCardSetCount(Bot bot, uint appId)
        {
            if (FullSetCount.TryGetValue(appId, out int value))
            {
                return value;
            }
            return await FetchCardSetCount(bot, appId).ConfigureAwait(false);
        }

        /// <summary>
        /// 读取缓存的每个游戏的每套卡牌张数
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="appId"></param>
        /// <returns>-1网络错误 0无卡牌</returns>
        private static async Task<int> FetchCardSetCount(Bot bot, uint appId)
        {
            Uri request = new(SteamCommunityURL, $"/profiles/{bot.SteamID}/gamecards/{appId}/");

            var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

            if (response?.Content == null)
            {
                return -1;
            }

            if (response.FinalUri.PathAndQuery.EndsWith("badges"))
            {
                FullSetCount.TryAdd(appId, 0);
                return 0;
            }

            if (response.Content.QuerySelector("div.badge_detail_tasks") == null)
            {
                return -1;
            }
            else
            {
                int count = response.Content.QuerySelectorAll("div.badge_card_set_card").Length;
                FullSetCount.TryAdd(appId, count);
                return count;
            }
        }
    }
}
