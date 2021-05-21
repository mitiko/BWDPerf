using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BWDPerf.Tools
{
    public static class ByteStreamHelper
    {
        public static async Task<uint> GetUInt32Async(IAsyncEnumerator<byte> enumerator)
        {
            var uint32Arr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                await enumerator.MoveNextAsync();
                uint32Arr[i] = enumerator.Current;
            }
            return BitConverter.ToUInt32(uint32Arr);
        }

        public static async Task<int> GetInt32Async(IAsyncEnumerator<byte> enumerator)
        {
            var int32Arr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                await enumerator.MoveNextAsync();
                int32Arr[i] = enumerator.Current;
            }
            return BitConverter.ToInt32(int32Arr);
        }
    }
}