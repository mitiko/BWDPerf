using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Common.Tools
{
    public class MeasureEntropy : ICoder<byte, byte>
    {
        private OccurenceDictionary<byte> OD { get; } = new();

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<byte> input)
        {
            System.Console.WriteLine("Started measuring");
            await foreach (var symbol in input)
            {
                this.OD.Add(symbol);
                yield return symbol;
            }
            System.Console.WriteLine("Measured");

            double total = this.OD.Sum();
            var entropy = this.OD.Values
                .Select(x => x / total)
                .Select(x => - Math.Log2(x) * x)
                .Sum();
            System.Console.WriteLine("Ended measuring");
            Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}");
        }
    }
}