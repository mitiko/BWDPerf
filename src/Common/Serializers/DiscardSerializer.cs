using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Serializers
{
    public class DiscardSerializer : ISerializer
    {
        public async Task Complete(IAsyncEnumerable<byte> input)
        {
            await foreach (var symbol in input) { }
        }
    }
}