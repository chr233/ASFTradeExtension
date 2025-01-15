using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Exchange;
using ASFTradeExtension.Data.Core;
using static ArchiSteamFarm.Steam.Exchange.ParseTradeResult;

namespace ASFTradeExtension.Core;

public class InventoryHandler(Bot _bot)
{
    /// <summary>
    /// 处于交易中的物品资源ID
    /// </summary>
    private HashSet<ulong> InTradeItemAssetIDs { get; set; } = [];

    /// <summary>
    /// 机器人库存缓存
    /// </summary>
    private List<Asset> InventoryCache { get; set; } = [];

    /// <summary>
    /// 机器人闪卡库存缓存
    /// </summary>
    private List<Asset> FoilInventoryCache { get; } = [];

    /// <summary>
    /// 卡牌套数信息缓存
    /// </summary>
    private Dictionary<uint, AssetBundle> CardSetCache { get; set; } = [];

    /// <summary>
    /// 闪卡套数信息缓存
    /// </summary>
    private Dictionary<uint, AssetBundle> FoilCardSetCache { get; set; } = [];

    /// <summary>
    /// 宝珠信息缓存
    /// </summary>
    private GemsInfo GemsInfoCache { get; set; } = new();

    private string? TradeLink { get; set; }

    /// <summary>
    /// 缓存更新时间
    /// </summary>
    private DateTime UpdateTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 缓存是否过期
    /// </summary>
    private bool NeedUpdate => DateTime.Now - UpdateTime > TimeSpan.FromSeconds(Config.CacheTTL);

    /// <summary>
    /// 更新机器人库存缓存
    /// </summary>
    /// <returns></returns>
    private async Task<bool> ReloadBotCache()
    {
        try
        {
            InventoryCache.Clear();

            if (!_bot.IsConnectedAndLoggedOn)
            {
                return false;
            }

            var inventory = await _bot.ArchiWebHandler.GetInventoryAsync().ToListAsync().ConfigureAwait(false);

            var tmpInTradeList = new HashSet<ulong>();

            var gemsInfo = new GemsInfo();

            foreach (var asset in inventory)
            {
                switch (asset.Type)
                {
                    case EAssetType.TradingCard:

                        if (!InTradeItemAssetIDs.Contains(asset.AssetID))
                        {
                            InventoryCache.Add(asset);
                        }
                        else
                        {
                            tmpInTradeList.Add(asset.AssetID);
                        }

                        break;

                    case EAssetType.FoilTradingCard:
                        if (!InTradeItemAssetIDs.Contains(asset.AssetID))
                        {
                            FoilInventoryCache.Add(asset);
                        }
                        else
                        {
                            tmpInTradeList.Add(asset.AssetID);
                        }

                        break;

                    case EAssetType.SteamGems:

                        if (asset.ClassID == 667924416)
                        {
                            gemsInfo.GemAssets.Add(asset);
                        }
                        else if (asset.ClassID == 667933237)
                        {
                            gemsInfo.BagAssets.Add(asset);
                        }

                        break;
                }
            }

            GemsInfoCache = gemsInfo;
            InTradeItemAssetIDs = tmpInTradeList;
            CardSetCache = await GetAppCardGroupLazyLoad(InventoryCache).ConfigureAwait(false);
            FoilCardSetCache = await GetAppCardGroupLazyLoad(FoilInventoryCache).ConfigureAwait(false);

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
    /// <param name="forceReload"></param>
    /// <returns></returns>
    public async Task<List<Asset>?> GetBotInventory(bool forceReload)
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
    public async Task<Dictionary<uint, AssetBundle>> GetCardSetCache(bool forceReload)
    {
        if (NeedUpdate || forceReload)
        {
            await ReloadBotCache().ConfigureAwait(false);
        }

        return CardSetCache;
    }

    /// <summary>
    /// 获取闪卡牌套数信息
    /// </summary>
    /// <param name="forceReload"></param>
    /// <returns></returns>
    public async Task<Dictionary<uint, AssetBundle>> GetFoilCardSetCache(bool forceReload)
    {
        if (NeedUpdate || forceReload)
        {
            await ReloadBotCache().ConfigureAwait(false);
        }

        return FoilCardSetCache;
    }

    /// <summary>
    /// 获取宝珠信息缓存
    /// </summary>
    /// <param name="forceReload"></param>
    /// <returns></returns>
    public async Task<GemsInfo> GetGemsInfoCache(bool forceReload)
    {
        if (NeedUpdate || forceReload)
        {
            await ReloadBotCache().ConfigureAwait(false);
        }

        return GemsInfoCache;
    }

    /// <summary>
    /// 获取AppID列表
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    private static HashSet<uint> GetAppIds(List<Asset> inventory)
    {
        var appIds = new HashSet<uint>();
        uint lastAppId = 0;

        foreach (var inv in inventory)
        {
            if (lastAppId != inv.RealAppID)
            {
                appIds.Add(inv.RealAppID);
                lastAppId = inv.RealAppID;
            }
        }

        return appIds;
    }

    /// <summary>
    /// 获取卡牌套数信息, 懒加载
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    private async Task<Dictionary<uint, AssetBundle>> GetAppCardGroupLazyLoad(List<Asset> inventory)
    {
        //卡牌套数字段
        var assetBundleDict = new Dictionary<uint, AssetBundle>();

        var subPath = await _bot.GetProfileLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(subPath))
        {
            return assetBundleDict;
        }

        var appIds = GetAppIds(inventory);

        var appClassIDsDict = new Dictionary<uint, HashSet<ulong>>();

        //int i = 0;
        foreach (var appId in appIds)
        {
            var cardSetCount = Utils.CardSetCache.GetCardSetCountFromCache(appId);
            assetBundleDict[appId] = new AssetBundle(appId, cardSetCount);
            appClassIDsDict[appId] = [];
        }

        foreach (var asset in inventory)
        {
            if (!assetBundleDict.TryGetValue(asset.RealAppID, out var bundle) ||
                !appClassIDsDict.TryGetValue(asset.RealAppID, out var classIDs))
            {
                continue;
            }

            bundle.Assets ??= [];

            bundle.Assets.Add(asset);
            classIDs.Add(asset.ClassID);
        }

        foreach (var (appId, bundle) in assetBundleDict)
        {
            //跳过没有缓存的AppID和未加载卡牌套数信息的AppID
            if (!appClassIDsDict.ContainsKey(appId) || !bundle.Loaded)
            {
                continue;
            }

            //可交易clsId张数
            var tradableSet = new Dictionary<ulong, int>();
            //所有clsId张数
            var nonTradableSet = new Dictionary<ulong, int>();

            if (bundle.Assets != null)
            {
                //按clsId统计卡牌张数
                foreach (var asset in bundle.Assets)
                {
                    var clsId = asset.ClassID;


                    if (asset.Tradable)
                    {
                        Increase(tradableSet, clsId);
                    }
                    else
                    {
                        Increase(nonTradableSet, clsId);
                    }
                }
            }

            //统计套数信息
            var tradableCount = tradableSet.Count == bundle.CardCountPerSet ? tradableSet.Values.Min() : 0;
            var nonTradableCount = nonTradableSet.Count == bundle.CardCountPerSet ? nonTradableSet.Values.Min() : 0;

            var extraTradableCount = tradableSet.Values.Sum() - (bundle.CardCountPerSet * tradableCount);
            var extraNonTradableCount = nonTradableSet.Values.Sum() - (bundle.CardCountPerSet * nonTradableCount);

            bundle.TradableSetCount = tradableCount;
            bundle.NonTradableSetCount = nonTradableCount;
            bundle.ExtraTradableCount = extraTradableCount;
            bundle.ExtraNonTradableCount = extraNonTradableCount;
        }

        return assetBundleDict;
    }

    public Task FullLoadAppCardGroup(Dictionary<uint, AssetBundle> bundles)
    {
        return FullLoadAppCardGroup(bundles.Values);
    }

    /// <summary>
    /// 获取卡牌套数信息, 完整加载整个库存
    /// </summary>
    /// <param name="bundles"></param>
    /// <returns></returns>
    public async Task FullLoadAppCardGroup(IEnumerable<AssetBundle> bundles)
    {
        //卡牌套数字段
        var lazyLoadBundles = new List<AssetBundle>();
        foreach (var bundle in bundles)
        {
            if (!bundle.Loaded)
            {
                lazyLoadBundles.Add(bundle);
            }
        }

        if (lazyLoadBundles.Count == 0)
        {
            return;
        }

        var oldCacheCount = Utils.CardSetCache.CacheCount;

        var subPath = await _bot.GetProfileLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(subPath))
        {
            return;
        }

        var semaphore = new SemaphoreSlim(5, 5);
        var countPerSets = await Utilities
            .InParallel(lazyLoadBundles.Select(bundle =>
                Utils.CardSetCache.GetCardSetCount(_bot, subPath, bundle.AppId, semaphore))).ConfigureAwait(false);

        //缓存有更新, 写入文件
        if (oldCacheCount != Utils.CardSetCache.CacheCount)
        {
            await Utils.CardSetCache.SaveCacheFile().ConfigureAwait(false);
        }

        //防止越界访问
        if (countPerSets.Count < lazyLoadBundles.Count)
        {
            return;
        }

        var i = 0;
        foreach (var bundle in lazyLoadBundles)
        {
            var setCount = countPerSets[i++];

            if (setCount >= 5)
            {
                bundle.CardCountPerSet = setCount;

                //可交易clsId张数
                var tradableSet = new Dictionary<ulong, int>();
                //所有clsId张数
                var nonTradableSet = new Dictionary<ulong, int>();

                if (bundle.Assets != null)
                {
                    //按clsId统计卡牌张数
                    foreach (var asset in bundle.Assets)
                    {
                        var clsId = asset.ClassID;

                        if (asset.Tradable)
                        {
                            Increase(tradableSet, clsId);
                        }
                        else
                        {
                            Increase(nonTradableSet, clsId);
                        }
                    }
                }

                //统计套数信息
                var tradableCount = tradableSet.Count == bundle.CardCountPerSet ? tradableSet.Values.Min() : 0;
                var nonTradableCount = nonTradableSet.Count == bundle.CardCountPerSet ? nonTradableSet.Values.Min() : 0;

                var extraTradableCount = tradableSet.Values.Sum() - (bundle.CardCountPerSet * tradableCount);
                var extraNonTradableCount = nonTradableSet.Values.Sum() - (bundle.CardCountPerSet * nonTradableCount);

                bundle.TradableSetCount = tradableCount;
                bundle.NonTradableSetCount = nonTradableCount;
                bundle.ExtraTradableCount = extraTradableCount;
                bundle.ExtraNonTradableCount = extraNonTradableCount;
            }
        }
    }

    /// <summary>
    /// 获取卡牌套数信息, 完整加载整个库存
    /// </summary>
    /// <param name="bundle"></param>
    /// <param name="foilBundle"></param>
    /// <returns></returns>
    public async Task LoadEventAppCardGroup(AssetBundle? bundle, AssetBundle? foilBundle)
    {
        if (bundle?.Loaded == true && foilBundle?.Loaded == true)
        {
            return;
        }

        var oldCacheCount = Utils.CardSetCache.CacheCount;

        var subPath = await _bot.GetProfileLink().ConfigureAwait(false);
        if (string.IsNullOrEmpty(subPath))
        {
            return;
        }

        var semaphore = new SemaphoreSlim(5, 5);
        var setCount = await Utils.CardSetCache.GetCardSetCount(_bot, subPath, SaleEventAppId, semaphore)
            .ConfigureAwait(false);

        //缓存有更新, 写入文件
        if (oldCacheCount != Utils.CardSetCache.CacheCount)
        {
            await Utils.CardSetCache.SaveCacheFile().ConfigureAwait(false);
        }

        if (setCount >= 5)
        {
            if (bundle != null)
            {
                bundle.CardCountPerSet = setCount;

                //可交易clsId张数
                var tradableSet = new Dictionary<ulong, int>();
                //所有clsId张数
                var nonTradableSet = new Dictionary<ulong, int>();

                if (bundle.Assets != null)
                {
                    //按clsId统计卡牌张数
                    foreach (var asset in bundle.Assets)
                    {
                        var clsId = asset.ClassID;

                        if (asset.Tradable)
                        {
                            Increase(tradableSet, clsId);
                        }
                        else
                        {
                            Increase(nonTradableSet, clsId);
                        }
                    }
                }

                //统计套数信息
                var tradableCount = tradableSet.Count == bundle.CardCountPerSet ? tradableSet.Values.Min() : 0;
                var nonTradableCount = nonTradableSet.Count == bundle.CardCountPerSet ? nonTradableSet.Values.Min() : 0;

                var extraTradableCount = tradableSet.Values.Sum() - (bundle.CardCountPerSet * tradableCount);
                var extraNonTradableCount = nonTradableSet.Values.Sum() - (bundle.CardCountPerSet * nonTradableCount);

                bundle.TradableSetCount = tradableCount;
                bundle.NonTradableSetCount = nonTradableCount;
                bundle.ExtraTradableCount = extraTradableCount;
                bundle.ExtraNonTradableCount = extraNonTradableCount;
            }

            if (foilBundle != null)
            {
                foilBundle.CardCountPerSet = setCount;

                //可交易clsId张数
                var tradableSet = new Dictionary<ulong, int>();
                //所有clsId张数
                var nonTradableSet = new Dictionary<ulong, int>();

                if (foilBundle.Assets != null)
                {
                    //按clsId统计卡牌张数
                    foreach (var asset in foilBundle.Assets)
                    {
                        var clsId = asset.ClassID;

                        if (asset.Tradable)
                        {
                            Increase(tradableSet, clsId);
                        }
                        else
                        {
                            Increase(nonTradableSet, clsId);
                        }
                    }
                }

                //统计套数信息
                var tradableCount = tradableSet.Count == foilBundle.CardCountPerSet ? tradableSet.Values.Min() : 0;
                var nonTradableCount =
                    nonTradableSet.Count == foilBundle.CardCountPerSet ? nonTradableSet.Values.Min() : 0;

                var extraTradableCount = tradableSet.Values.Sum() - (foilBundle.CardCountPerSet * tradableCount);
                var extraNonTradableCount =
                    nonTradableSet.Values.Sum() - (foilBundle.CardCountPerSet * nonTradableCount);

                foilBundle.TradableSetCount = tradableCount;
                foilBundle.NonTradableSetCount = nonTradableCount;
                foilBundle.ExtraTradableCount = extraTradableCount;
                foilBundle.ExtraNonTradableCount = extraNonTradableCount;
            }
        }
    }

    /// <summary>
    /// 增加1
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    private static void Increase<T>(Dictionary<T, int> dict, T key) where T : notnull
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = 0;
        }

        dict[key] = value + 1;
    }

    /// <summary>
    /// 设置缓存立即过期
    /// </summary>
    public void ExpiredCache()
    {
        UpdateTime = DateTime.MinValue;
    }

    /// <summary>
    /// 添加交易中物品列表
    /// </summary>
    /// <param name="assets"></param>
    public async Task AddInTradeItems(IEnumerable<Asset> assets)
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
        CardSetCache = await GetAppCardGroupLazyLoad(InventoryCache).ConfigureAwait(false);
    }

    /// <summary>
    /// 新增交易中物品的
    /// </summary>
    /// <param name="tradeResult"></param>
    public void AddInTradeItems(ParseTradeResult tradeResult)
    {
        if (tradeResult.Result == EResult.Accepted)
        {
            if (tradeResult.ItemsToReceive != null)
            {
                foreach (var asset in tradeResult.ItemsToReceive)
                {
                    InventoryCache.Add(asset);
                    InTradeItemAssetIDs.Add(asset.AssetID);
                }
            }

            if (tradeResult.ItemsToGive != null)
            {
                foreach (var asset in tradeResult.ItemsToGive)
                {
                    InTradeItemAssetIDs.Add(asset.AssetID);
                }
            }
        }
    }

    /// <summary>
    /// 读取交易链接
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetTradeLink()
    {
        if (!string.IsNullOrEmpty(TradeLink))
        {
            return TradeLink;
        }

        var request = new Uri(SteamCommunityURL, $"/profiles/{_bot.SteamID}/tradeoffers/privacy");
        var response = await _bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL)
            .ConfigureAwait(false);
        if (response?.Content == null)
        {
            return null;
        }

        var inputEle = response.Content.SelectSingleNode<IElement>("//input[@id='trade_offer_access_url']");

        TradeLink = inputEle?.GetAttribute("value");
        return TradeLink;
    }

    /// <summary>
    /// 查找用户
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public async Task<FindUserData?> GetUserBasicInfo(ulong steamId)
    {
        var request = new Uri(SteamCommunityURL, $"/profiles/{steamId}?xml=1");
        var response = await _bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        var root = response?.Content;
        if (root == null)
        {
            return null;
        }

        var strSteamId64 = root.QuerySelector("steamID64")?.TextContent;
        string? profilePath = null;
        if (strSteamId64 == null || !ulong.TryParse(strSteamId64, out var steamId64))
        {
            steamId64 = 0;
        }
        else
        {
            var customUrl = root.QuerySelector("customURL")?.TextContent;
            profilePath = string.IsNullOrEmpty(customUrl) ? $"profiles/{steamId64}" : $"id/{customUrl}";
        }

        var result = new FindUserData(steamId64, profilePath);
        return result;
    }


    /// <summary>
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="profilePath"></param>
    /// <param name="page"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    private async Task<IDocument?> FetchBadgePage(string profilePath, int page = 1, CancellationToken cancel = default)
    {
        try
        {
            var request = new Uri(SteamCommunityURL, $"/{profilePath}/badges/?sort=c&p={page}&l=schinese");
            var response = await _bot.ArchiWebHandler
                .UrlGetToHtmlDocumentWithSession(request, cancellationToken: cancel).ConfigureAwait(false);

            return response?.Content;
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            return null;
        }
    }

    /// <summary>
    /// 提取徽章信息
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static Dictionary<uint, byte> ParseToBadges(IDocument document)
    {
        Dictionary<uint, byte> badges = [];

        var badgeEles = document.QuerySelectorAll("div.badge_row.is_link");
        foreach (var ele in badgeEles)
        {
            var url = ele.QuerySelector("a.badge_row_overlay")?.GetAttribute("href");
            if (url == null)
            {
                continue;
            }

            var match = RegexUtils.MatchGameCards.Match(url);
            if (!match.Success || !uint.TryParse(match.Groups[1].Value, out var appId))
            {
                continue;
            }

            var strLevel = ele.QuerySelector(".badge_info_description>div:nth-child(2)")?.TextContent.Trim() ?? "";
            match = RegexUtils.MatchBadgeLevel.Match(strLevel);

            if (!match.Success || !byte.TryParse(match.Groups[1].Value, out var level))
            {
                continue;
            }

            badges.TryAdd(appId, level);
        }

        return badges;
    }

    /// <summary>
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="profilePath"></param>
    /// <param name="fullLoad"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<UserBadgeInfo?> GetUserBadgeSummary(string profilePath, bool fullLoad = false,
        CancellationToken cancellation = default)
    {
        var document = await FetchBadgePage(profilePath, 1, cancellation).ConfigureAwait(false);

        if (document == null)
        {
            return null;
        }

        var nickname = document.QuerySelector("span.profile_small_header_name > a")?.TextContent.Trim();
        var strLevel = document.QuerySelector("span.profile_xp_block_level span.friendPlayerLevelNum")?.TextContent
            .Trim();
        var strExp = document.QuerySelector("span.profile_xp_block_xp")?.TextContent.Trim().Replace(",", "");

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(strLevel) || string.IsNullOrEmpty(strExp))
        {
            return null;
        }

        if (!int.TryParse(strLevel, out var level))
        {
            level = -1;
        }

        var match = RegexUtils.MatchLevelExp.Match(strExp);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var exp))
        {
            exp = -1;
        }

        var badges = ParseToBadges(document);

        var strMaxPage = document.QuerySelector("div.pageLinks>a.pagelink:nth-last-child(2)")?.TextContent.Trim();
        if (!int.TryParse(strMaxPage, out var totalPage))
        {
            totalPage = 1;
        }

        // 加载后续页面
        if (fullLoad)
        {
            if (totalPage > 1)
            {
                List<Task<IDocument?>> tasks = [];
                for (var page = 2; page <= totalPage; page++)
                {
                    tasks.Add(FetchBadgePage(profilePath, page, cancellation));
                }

                var badgeDocuments = await Utilities.InParallel(tasks).ConfigureAwait(false);
                foreach (var bd in badgeDocuments)
                {
                    if (bd == null)
                    {
                        continue;
                    }

                    var bs = ParseToBadges(bd);
                    if (bs?.Count > 0)
                    {
                        foreach (var (k, v) in bs)
                        {
                            badges.TryAdd(k, v);
                        }
                    }
                }
            }
        }

        var result = new UserBadgeInfo(nickname, level, exp, totalPage, fullLoad, badges);
        return result;
    }

    /// <summary>
    /// 选择可用套卡
    /// </summary>
    /// <param name="badgeInfo"></param>
    /// <param name="targetSet"></param>
    /// <returns></returns>
    public async Task<LevelCardSetData> SelectFullSetCards(Dictionary<uint, byte> badgeInfo, int targetSet)
    {
        var invCache = await GetCardSetCache(false).ConfigureAwait(false);
        await FullLoadAppCardGroup(invCache).ConfigureAwait(false);

        var sortedInv = invCache
            .Select(static x => x.Value)
            .OrderByDescending(static x => x.TradableSetCount);

        List<Asset> offer = [];
        var currentSet = 0;
        var cardType = 0;

        foreach (var bundle in sortedInv)
        {
            if (bundle.TradableSetCount == 0)
            {
                continue;
            }

            if (!badgeInfo.TryGetValue(bundle.AppId, out var currentLevel))
            {
                currentLevel = 0;
            }

            var setCount = Math.Min(5 - currentLevel, bundle.TradableSetCount);
            setCount = Math.Min(targetSet - currentSet, setCount);

            if (setCount > 0)
            {
                var flag = bundle.Assets
                    .Select(static x => x.ClassID)
                    .Distinct()
                    .ToDictionary(static x => x, _ => setCount);

                foreach (var asset in bundle.Assets)
                {
                    var clsId = asset.ClassID;
                    if (flag[clsId] > 0)
                    {
                        offer.Add(asset);
                        flag[clsId]--;
                    }
                }

                cardType++;
                currentSet += setCount;

                if (targetSet == currentSet)
                {
                }
            }
        }

        return new LevelCardSetData(currentSet, cardType, offer);
    }
}