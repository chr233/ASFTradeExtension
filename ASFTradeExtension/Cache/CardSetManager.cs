using ArchiSteamFarm.Steam;
using ASFTradeExtension.Data;
using Newtonsoft.Json;
using SteamKit2.Internal;
using System.Collections.Concurrent;
using System.Text;

namespace ASFTradeExtension.Cache;

internal static class CardSetManager
{
    private static CardCache FullSetCount { get; set; } = new();

    public static async Task<int> GetCardSetCount(Bot bot, uint appId)
    {
        if (FullSetCount.TryGetValue(appId, out var value))
        {
            return value;
        }
        return await FetchCardSetCount(bot, appId).ConfigureAwait(false);
    }

    private static async Task<int> FetchCardSetCount(Bot bot, uint appId)
    {
        var request = new Uri(SteamCommunityURL, $"/profiles/{bot.SteamID}/gamecards/{appId}/");

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

        var count = response.Content.QuerySelectorAll("div.badge_card_set_card").Length;
        FullSetCount.TryAdd(appId, count);
        return count;
    }

    /// <summary>
    /// 获取Cookies文件路径
    /// </summary>
    /// <returns></returns>
    private static string GetFilePath()
    {
        var pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
        var filePath = Path.Combine(pluginFolder, "CardSetCache.json");
        return filePath;
    }

    internal static async Task LoadCacheFile()
    {
        try
        {
            var filePath = GetFilePath();
            if (File.Exists(filePath))
            {
                using var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var raw = await sr.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(raw))
                {
                    var dict = JsonConvert.DeserializeObject<CardCache>(raw);
                    if (dict != null)
                    {
                        FullSetCount = dict;
                        return;
                    }
                }
            }
            await SaveCacheFile().ConfigureAwait(false);
        }
        catch (Exception)
        {
            ASFLogger.LogGenericError("读取缓存出错");
            await SaveCacheFile().ConfigureAwait(false);
        }
    }

    internal static async Task SaveCacheFile()
    {
        try
        {
            var filePath = GetFilePath();
            using var fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs);
            var json = JsonConvert.SerializeObject(FullSetCount);
            await sw.WriteAsync(json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex, "写入缓存出错");
        }
    }
}
