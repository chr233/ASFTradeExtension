using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using CardTradeExtension.Data;

namespace CardTradeExtension.Core
{
    internal static class Handler
    {
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
                var filtedInventory = inventory.Where(x => x.Type == Asset.EType.TradingCard || x.Type == Asset.EType.FoilTradingCard).ToList();
                return filtedInventory;
            }
            catch (HttpRequestException e)
            {
                ASFLogger.LogGenericWarningException(e);
                return null;
            }
            catch (Exception e)
            {
                ASFLogger.LogGenericException(e);
                return null;
            }
        }

        internal static IDictionary<uint, IList<Asset>> GroupCardsByAppId(IEnumerable<Asset> inventory)
        {
            Dictionary<uint, IList<Asset>> result = new();
            foreach (var item in inventory)
            {
                if (result.TryGetValue(item.RealAppID, out var list))
                {
                    list.Add(item);
                }
                else
                {
                    result.TryAdd(item.RealAppID, new List<Asset> { item });
                }
            }
            return result;
        }

        internal static async Task<IDictionary<uint, AssetBundle>> GetAppCardGroup(Bot bot, IList<uint> appIds, IEnumerable<Asset> inventory)
        {
            var countPerSets = await Utilities.InParallel(appIds.Select(appId => CacheHelper.GetCacheCardSetCount(bot, appId))).ConfigureAwait(false);

            Dictionary<uint, AssetBundle> result = new();

            if (countPerSets.Count >= appIds.Count)
            {
                for (int i = 0; i < appIds.Count; i++)
                {
                    uint appId = appIds[i];
                    int countPerSet = countPerSets[i];
                    var assets = inventory.Where(x => x.RealAppID == appId).ToList();


                }
            }

            return result;
        }




    }
}
