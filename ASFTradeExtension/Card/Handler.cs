using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Cache;
using ASFTradeExtension.Data;
using System.Collections.Concurrent;

namespace ASFTradeExtension.Card;

internal static class Handler
{
    internal static ConcurrentDictionary<string, HashSet<long>> InTradeItem { get; set; } = new();

    /// <summary>
    /// 读取机器人卡牌库存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<IEnumerable<Asset>?> FetchBotCards(Bot bot)
    {
        try
        {
            var inventory = await bot.ArchiWebHandler.GetInventoryAsync(0, 753, 6).ToListAsync().ConfigureAwait(false);
            var filtedInventory = inventory.Where(x => x.Type == Asset.EType.TradingCard).ToList();
            return filtedInventory;
        }
        catch (Exception e)
        {
            ASFLogger.LogGenericException(e);
            return null;
        }
    }

    /// <summary>
    /// 获取卡牌套数信息
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appIds"></param>
    /// <param name="inventory"></param>
    /// <returns></returns>
    internal static async Task<IDictionary<uint, AssetBundle>> GetAppCardGroup(Bot bot, IEnumerable<uint> appIds, IEnumerable<Asset> inventory)
    {
        var countPerSets = await Utilities.InParallel(appIds.Select(appId => CardSetManager.GetCardSetCount(bot, appId))).ConfigureAwait(false);

        Dictionary<uint, AssetBundle> result = new();

        if (countPerSets.Count >= appIds.Count())
        {
            for (int i = 0; i < appIds.Count(); i++)
            {
                uint appId = appIds.ElementAt(i);
                int countPerSet = countPerSets[i];

                int tradableSetCount, totalSetCount, extraTradableCount, extraTotalCount;

                IEnumerable<Asset>? assets = null;

                if (countPerSet > 0)
                {
                    assets = inventory.Where(x => x.RealAppID == appId);
                    var classIds = assets.Select(x => x.ClassID).Distinct();

                    if (classIds.Count() == countPerSet)
                    {
                        var tradableCountPerClassId = classIds.Select(x => assets.Where(y => y.Tradable && y.ClassID == x).Count());
                        var totalCountPerClassId = classIds.Select(x => assets.Where(y => y.ClassID == x).Count());
                        tradableSetCount = tradableCountPerClassId.Min();
                        totalSetCount = totalCountPerClassId.Min();
                        extraTradableCount = tradableCountPerClassId.Sum() - countPerSet * tradableSetCount;
                        extraTotalCount = totalCountPerClassId.Sum() - countPerSet * totalSetCount;
                    }
                    else
                    {
                        tradableSetCount = 0;
                        totalSetCount = 0;
                        extraTradableCount = assets.Count(x => x.Tradable);
                        extraTotalCount = assets.Count();
                    }
                }
                else
                {
                    tradableSetCount = 0;
                    totalSetCount = 0;
                    extraTradableCount = 0;
                    extraTotalCount = 0;
                }

                AssetBundle bundle = new()
                {
                    Assets = assets,
                    CardCountPerSet = countPerSet,
                    TradableSetCount = tradableSetCount,
                    TotalSetCount = totalSetCount,
                    ExtraTradableCount = extraTradableCount,
                    ExtraTotalCount = extraTotalCount,
                };

                result.TryAdd(appId, bundle);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取单个App的库存卡牌套数
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appIds"></param>
    /// <param name="inventory"></param>
    /// <returns></returns>
    internal static async Task<AssetBundle> GetAppCardBundle(Bot bot, uint appId, IEnumerable<Asset> inventory)
    {
        int countPerSet = await CardSetManager.GetCardSetCount(bot, appId).ConfigureAwait(false);
        int tradableSetCount, totalSetCount, extraTradableCount, extraTotalCount;

        IEnumerable<Asset>? assets = null;

        if (countPerSet > 0)
        {
            assets = inventory.Where(x => x.RealAppID == appId);
            var classIds = assets.Select(x => x.ClassID).Distinct();

            if (classIds.Count() == countPerSet)
            {
                var tradableCountPerClassId = classIds.Select(x => assets.Where(y => y.Tradable && y.ClassID == x).Count());
                var totalCountPerClassId = classIds.Select(x => assets.Where(y => y.ClassID == x).Count());
                tradableSetCount = tradableCountPerClassId.Min();
                totalSetCount = totalCountPerClassId.Min();
                extraTradableCount = tradableCountPerClassId.Sum() - countPerSet * tradableSetCount;
                extraTotalCount = totalCountPerClassId.Sum() - countPerSet * totalSetCount;
            }
            else
            {
                tradableSetCount = 0;
                totalSetCount = 0;
                extraTradableCount = assets.Count(x => x.Tradable);
                extraTotalCount = assets.Count();
            }
        }
        else
        {
            tradableSetCount = 0;
            totalSetCount = 0;
            extraTradableCount = 0;
            extraTotalCount = 0;
        }

        AssetBundle bundle = new()
        {
            Assets = assets,
            CardCountPerSet = countPerSet,
            TradableSetCount = tradableSetCount,
            TotalSetCount = totalSetCount,
            ExtraTradableCount = extraTradableCount,
            ExtraTotalCount = extraTotalCount,
        };

        return bundle;
    }
}
