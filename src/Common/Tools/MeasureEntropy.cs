using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Common.Entities;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Common.Tools
{
    public class MeasureEntropy : ICoder<byte, byte>, ICoder<DictionaryIndex, byte>
    {
        private OccurenceDictionary<byte> OD { get; } = new();

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<byte> input)
        {
            await foreach (var symbol in input)
            {
                this.OD.Add(symbol);
                yield return symbol;
            }

            double total = this.OD.Sum();
            var entropy = this.OD.Values
                .Select(x => x / total)
                .Select(x => - Math.Log2(x) * x)
                .Sum();
            Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}");
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<DictionaryIndex> input)
        {
            int count = 0;
            await foreach (var index in input)
            {
                byte symbol = (byte) index.Index;
                this.OD.Add(symbol);
                count++;
                yield return symbol;
            }

            double total = this.OD.Sum();
            var entropy = this.OD.Values
                .Select(x => x / total)
                .Select(x => - Math.Log2(x) * x)
                .Sum();
            Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}, Count: {count}");
        }
    }
}