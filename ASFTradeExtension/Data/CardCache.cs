using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFTradeExtension.Data;
internal sealed class CardCache : ConcurrentDictionary<uint, int>
{
}
