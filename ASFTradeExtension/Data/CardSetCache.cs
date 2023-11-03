using System.Collections.Concurrent;

namespace ASFTradeExtension.Data;
internal sealed class CardSetCache : ConcurrentDictionary<uint, int>
{
}
