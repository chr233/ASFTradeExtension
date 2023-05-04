using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using System.Collections.Concurrent;

namespace CardTradeExtension.CSGO;

internal static class Handler
{
    /// <summary>
    /// 读取机器人CS库存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<IEnumerable<Asset>?> FetchBotCSInventory(Bot bot, Func<Asset, bool>? func)
    {
        try
        {
            var inventory = await bot.ArchiWebHandler.GetInventoryAsync(0, 730, 2).ToListAsync().ConfigureAwait(false);
            if (func != null)
            {
                var filtedInventory = inventory.Where(func).ToList();
                return filtedInventory;
            }
            else
            {
                return inventory;
            }
        }
        catch (Exception e)
        {
            ASFLogger.LogGenericException(e);
            return null;
        }
    }

    /// <summary>
    /// tradeId, steamId
    /// </summary>
    private static ConcurrentDictionary<ulong, ulong> _verifiedTrades = new();

    internal static void AddTrade(ulong tradeId, ulong steamId)
    {
        _verifiedTrades.TryAdd(tradeId, steamId);
    }


    internal static bool IsMyTrade(ulong tradeId, ulong steamId)
    {
        return _verifiedTrades.TryGetValue(tradeId, out ulong value) && value == steamId;
    }


    internal static void RemoveMyTrade(ulong tradeId)
    {
        _verifiedTrades.TryRemove(tradeId, out _);
    }
}
