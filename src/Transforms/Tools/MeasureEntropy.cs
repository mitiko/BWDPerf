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

            double sum = this.OD.Sum();
            double entropy = 0;
            foreach (var freq in this.OD.Values)
                entropy -= (freq / sum) * Math.Log2(freq / sum);
            Console.WriteLine($"[{this.GetHashCode()}] Entropy: {entropy}; Count: {this.OD.Count}");
            this.OD.Clear();
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

                // Calculate stream entropy
                double sum = this.ODInt.Sum();
                double entropy = 0;
                foreach (var freq in this.ODInt.Values)
                    entropy -= (freq / sum) * Math.Log2(freq / sum);

                // Calculate dictionary entropy
                var dictionary = block.Dictionary.Serialize();
                for (int i = 0; i < dictionary.Length; i++)
                    this.OD.Add(dictionary.Span[i]);

                sum = this.OD.Sum();
                double dictEntropy = 0;
                foreach (var freq in this.OD.Values)
                    dictEntropy -= (freq / sum) * Math.Log2(freq / sum);

                Console.WriteLine($"[{this.GetHashCode()}] Stream: {entropy}; Stream length: {block.Stream.Length}; Distinct symbols: {this.ODInt.Count}");
                Console.WriteLine($"[{this.GetHashCode()}] Dictionary: {dictEntropy}; Dictionary length: {dictionary.Length}");
                Console.WriteLine($"[{this.GetHashCode()}] Total size estimation: {(dictionary.Length * dictEntropy + block.Stream.Length * entropy) / 8} bytes");
                yield return block;
                this.ODInt.Clear();
                this.OD.Clear();
            }
        }
    }
}