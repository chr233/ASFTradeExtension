using ArchiSteamFarm.Steam;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace ASFTradeExtension.Cache;

/// <summary>
/// 卡牌套数管理类
/// </summary>
internal class CardSetManager
{
    /// <summary>
    /// 卡牌套数信息缓存
    /// </summary>
    private ConcurrentDictionary<uint, int> FullSetCountCache { get; set; } = new();

    /// <summary>
    /// 缓存数量
    /// </summary>
    internal int CacheCount => FullSetCountCache.Count;

    /// <summary>
    /// 获取卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="subPath"></param>
    /// <param name="appId"></param>
    /// <returns></returns>
    private async Task<int> FetchCardSetCount(Bot bot, string subPath, uint appId)
    {
        if (!subPath.EndsWith('/'))
        {
            subPath += '/';
        }
        var request = new Uri(SteamCommunityURL, $"{subPath}gamecards/{appId}/");

        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        if (response?.Content == null)
        {
            return -1;
        }

        if (response.FinalUri.PathAndQuery.EndsWith("badges"))
        {
            FullSetCountCache[appId] = 0;
            return 0;
        }

        if (response.Content.QuerySelector("div.badge_detail_tasks") == null)
        {
            return -1;
        }

        var count = response.Content.QuerySelectorAll("div.badge_card_set_card").Length;
        FullSetCountCache[appId] = count;
        return count;
    }

    /// <summary>
    /// 获取卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="subPath"></param>
    /// <param name="appId"></param>
    /// <param name="semaphore"></param>
    /// <returns></returns>
    internal async Task<int> GetCardSetCount(Bot bot, string subPath, uint appId, SemaphoreSlim semaphore)
    {
        if (FullSetCountCache.TryGetValue(appId, out var value))
        {
            return value;
        }

        try
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return await FetchCardSetCount(bot, subPath, appId).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 从缓存中读取卡牌套数
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    internal int GetCardSetCountFromCache(uint appId)
    {
        return FullSetCountCache.TryGetValue(appId, out var value) ? value : -1;
    }

    /// <summary>
    /// 获取库存缓存文件路径
    /// </summary>
    /// <returns></returns>
    private static string GetFilePath()
    {
        var pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
        var filePath = Path.Combine(pluginFolder, "ATE_Cache.json");
        return filePath;
    }

    /// <summary>
    /// 加载缓存文件
    /// </summary>
    /// <returns></returns>
    internal async Task LoadCacheFile()
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
                    var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<uint, int>>(raw);
                    if (dict != null)
                    {
                        FullSetCountCache = dict;
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

    /// <summary>
    /// 保存缓存文件
    /// </summary>
    /// <returns></returns>
    internal async Task SaveCacheFile()
    {
        try
        {
            var filePath = GetFilePath();
            using var fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs);
            var json = JsonConvert.SerializeObject(FullSetCountCache);
            await sw.WriteAsync(json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex, "写入缓存出错");
        }
    }
}
