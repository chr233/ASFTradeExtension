using ArchiSteamFarm.Steam.Data;
using System.Collections.Concurrent;

namespace ASFTradeExtension.Cache;

internal static class InTradeAssetManager
{
    private static ConcurrentDictionary<ulong, Asset> InTradeItems { get; set; } = new();

    internal static void AddInTradeItem(Asset asset)
    {
        InTradeItems.TryAdd(asset.InstanceID, asset);
    }

    internal static void AddInTradeItems(IEnumerable<Asset> assets)
    {
        foreach (var asset in assets)
        {
            InTradeItems.TryAdd(asset.InstanceID, asset);
        }
    }

    internal static void RemoveInTradeItem(Asset asset)
    {
        InTradeItems.TryRemove(asset.InstanceID, out _);
    }

    internal static void RemoveInTradeItems(IEnumerable<Asset> assets)
    {
        foreach (var asset in assets)
        {
            InTradeItems.TryRemove(asset.InstanceID, out _);
        }
    }

    internal static IEnumerable<Asset> ExcludeInTradeItems(IEnumerable<Asset> assets)
    {
        var filteredAsset = new List<Asset>();

        foreach (var asset in assets)
        {
            if (!InTradeItems.ContainsKey(asset.InstanceID))
            {
                filteredAsset.Add(asset);
            }
        }

        return filteredAsset;
    }
}
