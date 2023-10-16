using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data;
using System.Collections.Concurrent;

namespace ASFTradeExtension.Csgo;

internal static class Handler
{
    /// <summary>
    /// 读取机器人CS库存
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="func"></param>
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
            Utils.ASFLogger.LogGenericException(e);
            return null;
        }
    }

    internal static async Task<Dictionary<CsgoItemType, List<Asset>>?> GetCsgoInventory(Bot bot)
    {
        try
        {
            var itemDict = new Dictionary<CsgoItemType, List<Asset>>();
            var inv = await bot.ArchiWebHandler.GetInventoryAsync(0, 730, 2).ToListAsync().ConfigureAwait(false);
            foreach (var item in inv)
            {
                var type = CsgoItemType.Other;
                if (item.Tags != null)
                {
                    foreach (var tag in item.Tags)
                    {
                        if (tag.Identifier == "Type")
                        {
                            switch (tag.Value)
                            {
                                case "CSGO_Type_WeaponCase":
                                    type = CsgoItemType.WeaponCase;
                                    break;
                                case "CSGO_Type_SniperRifle":
                                case "CSGO_Type_Rifle":
                                case "CSGO_Type_Shotgun":
                                case "CSGO_Type_SMG":
                                case "CSGO_Type_Machinegun":
                                case "CSGO_Type_Pistol":
                                    type = CsgoItemType.Weapon;
                                    break;
                                case "CSGO_Type_Collectible":
                                    type = CsgoItemType.Collectible;
                                    break;
                                case "CSGO_Type_MusicKit":
                                    type = CsgoItemType.MusicKit;
                                    break;
                                case "CSGO_Type_Tool":
                                    type = CsgoItemType.Tool;
                                    break;
                                case "Type_CustomPlayer":
                                    type = CsgoItemType.Player;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }
                    }
                }

                if (itemDict.TryGetValue(type, out var itemList))
                {
                    itemList.Add(item);
                }
                else
                {
                    itemDict.Add(type, new List<Asset> { item });
                }
            }

            return itemDict;
        }
        catch (Exception ex)
        {
            Utils.ASFLogger.LogGenericException(ex);
            return null;
        }
    }

    /// <summary>
    /// tradeId, steamId
    /// </summary>
    private static ConcurrentDictionary<ulong, ulong> VerifiedTrades { get; } = new();

    internal static void AddTrade(ulong tradeId, ulong steamId)
    {
        VerifiedTrades.TryAdd(tradeId, steamId);
    }


    internal static bool IsMyTrade(ulong tradeId, ulong steamId)
    {
        return VerifiedTrades.TryGetValue(tradeId, out ulong value) && value == steamId;
    }


    internal static void RemoveMyTrade(ulong tradeId)
    {
        VerifiedTrades.TryRemove(tradeId, out _);
    }
}
