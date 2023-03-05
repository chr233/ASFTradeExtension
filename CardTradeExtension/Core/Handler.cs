using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

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
    }
}
