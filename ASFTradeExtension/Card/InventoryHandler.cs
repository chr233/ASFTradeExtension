using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Cache;
using ASFTradeExtension.Data;

namespace ASFTradeExtension.Core;
internal class InventoryHandler
{
    /// <summary>
    /// 当前机器人
    /// </summary>
    private Bot Bot { get; init; }

    /// <summary>
    /// 处于交易中的物品资源ID
    /// </summary>
    private HashSet<ulong> InTradeItemAssetIDs { get; set; }

    /// <summary>
    /// 机器人库存缓存
    /// </summary>
    private List<Asset> InventoryCache { get; set; }

    /// <summary>
    /// 卡牌套数信息缓存
    /// </summary>
    private Dictionary<uint, AssetBundle> CardSetCache { get; set; }

    /// <summary>
    /// 缓存更新时间
    /// </summary>
    private DateTime UpdateTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 缓存是否过期
    /// </summary>
    private bool NeedUpdate => DateTime.Now - UpdateTime > TimeSpan.FromMinutes(10);

    public InventoryHandler(Bot bot)
    {
        Bot = bot;
        InTradeItemAssetIDs = new HashSet<ulong>();
        InventoryCache = new List<Asset>();
        CardSetCache = new Dictionary<uint, AssetBundle>();
    }

    /// <summary>
    /// 更新机器人库存缓存
    /// </summary>
    /// <returns></returns>
    private async Task<bool> ReloadBotCache()
    {
        try
        {
            InventoryCache.Clear();

            var inventory = await Bot.ArchiWebHandler.GetInventoryAsync(0, 753, 6).ToListAsync().ConfigureAwait(false);

            var tmpInTradeList = new HashSet<ulong>();

            foreach (var asset in inventory)
            {
                if (asset.Type == Asset.EType.TradingCard)
                {
                    if (!InTradeItemAssetIDs.Contains(asset.AssetID))
                    {
                        InventoryCache.Add(asset);
                    }
                    else
                    {
                        tmpInTradeList.Add(asset.AssetID);
                    }
                }
            }

            InTradeItemAssetIDs = tmpInTradeList;
            CardSetCache = await GetAppCardGroup(InventoryCache).ConfigureAwait(false);

            UpdateTime = DateTime.Now;
            return true;
        }
        catch (Exception e)
        {
            ASFLogger.LogGenericException(e);
            return false;
        }
    }

    /// <summary>
    /// 获取机器人库存
    /// </summary>
    /// <param name="forceReload"></param >
    /// <returns></returns>
    internal async Task<List<Asset>?> GetBotInventory(bool forceReload)
    {
        if (NeedUpdate || forceReload)
        {
            await ReloadBotCache().ConfigureAwait(false);
        }

        return InventoryCache;
    }

    /// <summary>
    /// 获取卡牌套数信息
    /// </summary>
    /// <param name="forceReload"></param>
    /// <returns></returns>
    internal async Task<Dictionary<uint, AssetBundle>> GetCardSetCache(bool forceReload)
    {
        if (NeedUpdate || forceReload)
        {
            await ReloadBotCache().ConfigureAwait(false);
        }

        return CardSetCache;
    }

    /// <summary>
    /// 获取AppID列表
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    private static HashSet<uint> GetAppIds(List<Asset> inventory)
    {
        var appIds = new HashSet<uint>();
        foreach (var inv in inventory)
        {
            appIds.Add(inv.RealAppID);
        }
        return appIds;
    }

    /// <summary>
    /// 获取卡牌套数信息
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    private async Task<Dictionary<uint, AssetBundle>> GetAppCardGroup(List<Asset> inventory)
    {
        //卡牌套数字段
        var assetBundleDict = new Dictionary<uint, AssetBundle>();

        var subPath = await ValidProfileLink($"/profiles/{Bot.SteamID}").ConfigureAwait(false);
        if (string.IsNullOrEmpty(subPath))
        {
            return assetBundleDict;
        }

        var oldCacheCount = CardSetManager.CacheCount;

        var semaphore = new SemaphoreSlim(5, 5);
        var appIds = GetAppIds(inventory);
        var countPerSets = await Utilities.InParallel(appIds.Select(appId => CardSetManager.GetCardSetCount(Bot, subPath, appId, semaphore))).ConfigureAwait(false);

        //缓存有更新, 写入文件
        if (oldCacheCount != CardSetManager.CacheCount)
        {
            await CardSetManager.SaveCacheFile().ConfigureAwait(false);
        }

        //防止越界访问
        if (countPerSets.Count < appIds.Count)
        {
            return assetBundleDict;
        }

        var appClassIDsDict = new Dictionary<uint, HashSet<ulong>>();

        int i = 0;
        foreach (var appId in appIds)
        {
            var setCount = countPerSets[i++];

            if (setCount >= 5)
            {
                assetBundleDict[appId] = new AssetBundle
                {
                    Assets = new List<Asset>(),
                    CardCountPerSet = setCount,
                    TradableSetCount = 0,
                    NonTradableSetCount = 0,
                    ExtraTradableCount = 0,
                    ExtraNonTradableCount = 0,
                };

                appClassIDsDict[appId] = new HashSet<ulong>();
            }
        }

        foreach (var asset in inventory)
        {
            if (!assetBundleDict.TryGetValue(asset.RealAppID, out var bundle) || !appClassIDsDict.TryGetValue(asset.RealAppID, out var classIDs))
            {
                continue;
            }

            bundle.Assets.Add(asset);
            classIDs.Add(asset.ClassID);
        }

        foreach (var (appId, bundle) in assetBundleDict)
        {
            if (!appClassIDsDict.TryGetValue(appId, out var classIDs))
            {
                continue;
            }

            //可交易clsId张数
            var tradableSet = new Dictionary<ulong, int>();
            //所有clsId张数
            var nonTradableSet = new Dictionary<ulong, int>();

            //按clsId统计卡牌张数
            foreach (var asset in bundle.Assets)
            {
                var clsId = asset.ClassID;


                if (asset.Tradable)
                {
                    tradableSet.Increase(clsId);
                }
                else
                {
                    nonTradableSet.Increase(clsId);
                }
            }

            //统计套数信息
            var tradableCount = tradableSet.Count == bundle.CardCountPerSet ? tradableSet.Values.MinValue() : 0;
            var nonTradableCount = nonTradableSet.Count == bundle.CardCountPerSet ? nonTradableSet.Values.MinValue() : 0;

            var extraTradableCount = tradableSet.Values.SumValue() - bundle.CardCountPerSet * tradableCount;
            var extraNonTradableCount = nonTradableSet.Values.SumValue() - bundle.CardCountPerSet * nonTradableCount;

            bundle.TradableSetCount = tradableCount;
            bundle.NonTradableSetCount = nonTradableCount;
            bundle.ExtraTradableCount = extraTradableCount;
            bundle.ExtraNonTradableCount = extraNonTradableCount;
        }

        return assetBundleDict;
    }

    /// <summary>
    /// 设置缓存立即过期
    /// </summary>
    internal void ExpiredCache()
    {
        UpdateTime = DateTime.MinValue;
    }

    /// <summary>
    /// 添加交易中物品列表
    /// </summary>
    /// <param name="assets"></param>
    internal async Task AddInTradeItems(IEnumerable<Asset> assets)
    {
        foreach (var asset in assets)
        {
            InTradeItemAssetIDs.Add(asset.AssetID);
        }

        await UpdateBotCache().ConfigureAwait(false);
    }

    /// <summary>
    /// 更新机器人库存缓存
    /// </summary>
    /// <returns></returns>
    private async Task UpdateBotCache()
    {
        var inventory = new List<Asset>();

        foreach (var asset in InventoryCache)
        {
            if (!InTradeItemAssetIDs.Contains(asset.AssetID))
            {
                inventory.Add(asset);
            }
        }

        InventoryCache = inventory;
        CardSetCache = await GetAppCardGroup(InventoryCache).ConfigureAwait(false);
    }

    /// <summary>
    /// 验证个人资料链接
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal async Task<string?> ValidProfileLink(string path)
    {
        var request = new Uri(SteamCommunityURL, path);

        var response = await Bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        return ParseProfilePage(response?.Content) ? response!.FinalUri.PathAndQuery : null;
    }

    /// <summary>
    /// 解析个人资料页面
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static bool ParseProfilePage(IDocument? document)
    {
        if (document == null)
        {
            return false;
        }

        var errorNode = document.QuerySelector("#responsive_page_template_content > div.error_ctn");
        return errorNode == null;
    }
}
