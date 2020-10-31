using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Serializers
{
    public class DiscardSerializer<Symbol> : ISerializer<Symbol>
    {
        public async Task Complete(IAsyncEnumerable<Symbol> input)
        {
            await foreach (var symbol in input) { }
        }
    }
}