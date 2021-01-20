using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Tools
{
    public class MeasureEntropy : ICoder<byte, byte>
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
            Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}; Count: {this.OD.Count}");
        }
    }
}