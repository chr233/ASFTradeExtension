using AngleSharp.Dom;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Exchange;
using ArchiSteamFarm.Web.Responses;
using System.Text.RegularExpressions;

namespace CardTradeExtension.Core
{
    internal static class InventoryParser
    {
        private const int maxItems = byte.MaxValue;

        private static Dictionary<string, HashSet<Asset>> _botInventorys = new();
        private static Dictionary<string, DateTime> _botUpdateTime = new();

        
        public static HashSet<Asset> GetFullSetList(IReadOnlyCollection<Asset> inventory, IReadOnlyDictionary<(uint RealAppID, Asset.EType Type, Asset.ERarity Rarity), (uint SetsToExtract, byte ItemsPerSet)> amountsToExtract)
        {
            if ((inventory == null) || (inventory.Count == 0))
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if ((amountsToExtract == null) || (amountsToExtract.Count == 0))
            {
                throw new ArgumentNullException(nameof(amountsToExtract));
            }
            
            HashSet<Asset> result = new();
            Dictionary<(uint RealAppID, Asset.EType Type, Asset.ERarity Rarity), Dictionary<ulong, HashSet<Asset>>> itemsPerClassIDPerSet = inventory.GroupBy(static item => (item.RealAppID, item.Type, item.Rarity)).ToDictionary(static grouping => grouping.Key, static grouping => grouping.GroupBy(static item => item.ClassID).ToDictionary(static group => group.Key, static group => group.ToHashSet()));

            //foreach (((uint RealAppID, Asset.EType Type, Asset.ERarity Rarity) set, (uint setsToExtract, byte itemsPerSet)) in amountsToExtract.OrderBy(static kv => kv.Value.ItemsPerSet))
            //{
            //    if (!itemsPerClassIDPerSet.TryGetValue(set, out Dictionary<ulong, HashSet<Asset>>? itemsPerClassID))
            //    {
            //        continue;
            //    }

            //    if (itemsPerSet < itemsPerClassID.Count)
            //    {
            //        throw new InvalidOperationException($"{nameof(itemsPerSet)} < {nameof(itemsPerClassID)}");
            //    }

            //    if (itemsPerSet > itemsPerClassID.Count)
            //    {
            //        continue;
            //    }

            //    ushort maxSetsAllowed = (ushort)((maxItems - result.Count) / itemsPerSet);
            //    ushort realSetsToExtract = (ushort)Math.Min(setsToExtract, maxSetsAllowed);

            //    if (realSetsToExtract == 0)
            //    {
            //        break;
            //    }

            //    foreach (HashSet<Asset> itemsOfClass in itemsPerClassID.Values)
            //    {
            //        ushort classRemaining = realSetsToExtract;

            //        foreach (Asset item in itemsOfClass.TakeWhile(_ => classRemaining > 0))
            //        {
            //            if (item.Amount > classRemaining)
            //            {
            //                Asset itemToSend = item.CreateShallowCopy();
            //                itemToSend.Amount = classRemaining;
            //                result.Add(itemToSend);

            //                classRemaining = 0;
            //            }
            //            else
            //            {
            //                result.Add(item);

            //                classRemaining -= (ushort)item.Amount;
            //            }
            //        }
            //    }
            //}

            return result;
        }
    }
}
