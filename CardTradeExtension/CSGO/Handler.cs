using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using System.Collections.Concurrent;

namespace CardTradeExtension.CSGO;

internal static class Handler
{
    /// <summary>
    /// 读取机器人卡牌库存
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<IEnumerable<Asset>?> FetchBotCSInventory(Bot bot)
    {
        try
        {
            var inventory = await bot.ArchiWebHandler.GetInventoryAsync(0, 730, 2).ToListAsync().ConfigureAwait(false);
            var filtedInventory = inventory.Where(x => x.Tradable).ToList();
            return filtedInventory;
        }
        catch (Exception e)
        {
            ASFLogger.LogGenericException(e);
            return null;
        }
    }

    /// <summary>
    /// 获取CS物品组
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="inventory"></param>
    /// <returns></returns>
    internal static IDictionary<ulong, IEnumerable<Asset>> GetCSItemGroup(Bot bot, IEnumerable<Asset> inventory)
    {
        Dictionary<ulong, IEnumerable<Asset>> result = new();

        if (inventory.Any())
        {
            var classIds = inventory.Select(x => x.ClassID).Distinct();

            foreach (var classId in classIds)
            {
                var assets = inventory.Where(x => x.ClassID == classId);
                result.Add(classId, assets);
            }
        }

        return result;
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
