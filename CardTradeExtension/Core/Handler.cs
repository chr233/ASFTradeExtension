using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

namespace CardTradeExtension.Core
{
    internal static class Handler
    {
        /// <summary>
        /// 获取成套卡牌套数列表
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="keyword"></param>
        /// <param name="page"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        internal static async Task<string?> ResponseGetFullSetList(Bot bot, string? keyword, uint page = 1, uint num = 30)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            // We require to fetch whole inventory as a list here, as we need to know the order for calculating index and previousAssetID
            List<Asset> inventory;

            try
            {
                inventory = await bot.ArchiWebHandler.GetInventoryAsync(0, 753, 6).ToListAsync().ConfigureAwait(false);
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

            return "";
        }

        internal static async Task<string?> ResponseGetSetCountOfGame(Bot bot,uint appId)
        {
            if (!bot.IsConnectedAndLoggedOn)
            {
                return bot.FormatBotResponse(Strings.BotNotConnected);
            }

            // We require to fetch whole inventory as a list here, as we need to know the order for calculating index and previousAssetID
            List<Asset> inventory;

            try
            {
                inventory = await bot.ArchiWebHandler.GetInventoryAsync(0, 753, 6).ToListAsync().ConfigureAwait(false);
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

            return "";
        }
    }
}
