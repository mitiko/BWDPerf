using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Algorithms.BWD
{
    public class BWDDecoder : ICoder<byte[], byte[]>
    {
        public async IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            await foreach (var buffer in input)
            {
                
            }
            yield return default;
        }
    }
}