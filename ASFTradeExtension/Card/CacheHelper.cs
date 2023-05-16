using ArchiSteamFarm.Steam;
using System.Collections.Concurrent;


namespace ASFTradeExtension.Card;

internal static class CacheHelper
{
    private static ConcurrentDictionary<uint, int> FullSetCount { get; } = new();

    public static async Task<int> GetCacheCardSetCount(Bot bot, uint appId)
    {
        if (FullSetCount.TryGetValue(appId, out int value))
        {
            return value;
        }
        return await FetchCardSetCount(bot, appId).ConfigureAwait(false);
    }

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

        int count = response.Content.QuerySelectorAll("div.badge_card_set_card").Length;
        FullSetCount.TryAdd(appId, count);
        return count;
    }
}
