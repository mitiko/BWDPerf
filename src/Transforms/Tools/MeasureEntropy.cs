using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Tools
{
    public class MeasureEntropy : ICoder<byte, byte>, ICoder<BWDBlock, BWDBlock>
    {
        private OccurenceDictionary<byte> OD { get; } = new();
        private OccurenceDictionary<int> ODInt { get; } = new();

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

        public async IAsyncEnumerable<BWDBlock> Encode(IAsyncEnumerable<BWDBlock> input)
        {
            await foreach (var block in input)
            {
                for (int i = 0; i < block.Stream.Length; i++)
                {
                    var x = block.Stream[i];
                    this.ODInt.Add(x);
                }
                double total = this.ODInt.Sum();
                var entropy = this.ODInt.Values
                    .Select(x => x / total)
                    .Select(x => - Math.Log2(x) * x)
                    .Sum();
                var size = block.Dictionary.Serialize().Length;
                Console.WriteLine($"this many symbols {block.Stream.Length}");
                Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}; Count: {this.ODInt.Count}");
                Console.WriteLine($"dictionary takes {size} bytes");
                yield return block;
            }
        }
    }
}