using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Steam;
using ASFTradeExtension.Data.Plugin;
using System.Collections.Concurrent;

namespace ASFTradeExtension.Cache;

/// <summary>
/// 卡牌套数管理类
/// </summary>
internal class CardSetManager
{
    public CardSetManager()
    {
        const string fileName = "ATE_Cache.json";
        var pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
        FilePath = Path.Combine(pluginFolder, fileName);
    }

    private string FilePath { get; }

    /// <summary>
    /// 发货机器人名称
    /// </summary>
    public string? MasterBotName { get; private set; }

    /// <summary>
    /// 卡牌套数信息缓存
    /// </summary>
    private ConcurrentDictionary<uint, int> FullSetCountCache { get; set; } = new();

    /// <summary>
    /// 缓存数量
    /// </summary>
    internal int CacheCount => FullSetCountCache.Count;

    /// <summary>
    /// 写缓存锁
    /// </summary>
    private SemaphoreSlim CacheLock { get; } = new(1, 1);

    public Task SetMasterBotName(string botName)
    {
        MasterBotName = botName;
        return SaveCacheFile();
    }

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
            await SaveCacheFile().ConfigureAwait(false);
            return 0;
        }

        if (response.Content.QuerySelector("div.badge_detail_tasks") == null)
        {
            return -1;
        }

        var count = response.Content.QuerySelectorAll("div.badge_card_set_card").Length;
        FullSetCountCache[appId] = count;
        if (CacheLock.CurrentCount > 0)
        {
            await SaveCacheFile().ConfigureAwait(false);
        }

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
            var count = await FetchCardSetCount(bot, subPath, appId).ConfigureAwait(false);

            return count;
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
        return FullSetCountCache.GetValueOrDefault(appId, -1);
    }

    /// <summary>
    /// 加载缓存文件
    /// </summary>
    /// <returns></returns>
    internal async Task LoadCacheFile()
    {
        try
        {
            await CacheLock.WaitAsync().ConfigureAwait(false);

            if (File.Exists(FilePath))
            {
                await using var fs = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var raw = await sr.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(raw))
                {
                    var data = raw.ToJsonObject<StorageData>();
                    if (data != null)
                    {
                        MasterBotName = data.MasterBotName;
                        FullSetCountCache = data.FullSetCount ?? [];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError("读取缓存出错");
        }
        finally
        {
            CacheLock.Release();
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
            await CacheLock.WaitAsync().ConfigureAwait(false);

            await using var fs = File.Open(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            await using var sw = new StreamWriter(fs);
            var data = new StorageData(MasterBotName, FullSetCountCache);
            var json = data.ToJsonText();
            await sw.WriteAsync(json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            ASFLogger.LogGenericError("写入缓存出错");
        }
        finally
        {
            CacheLock.Release();
        }
    }
}