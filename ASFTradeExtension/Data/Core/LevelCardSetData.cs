using ArchiSteamFarm.Steam.Data;
using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Core;

public sealed record LevelCardSetData
{
    public LevelCardSetData(int cardSet, int cardType, List<Asset> tradeItems)
    {
        CardSet = cardSet;
        CardType = cardType;
        TradeItems = tradeItems;
    }

    public int CardSet { get; init; }
    public int CardType { get; set; }
    public int CardCount => TradeItems.Count;

    [JsonIgnore]
    public List<Asset> TradeItems { get; init; }
}