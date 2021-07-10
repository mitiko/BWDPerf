using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Tools
{
    public class MeasureEntropy :
        ICoder<byte, byte>,
        ICoder<ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>>,
        ICoder<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
    {
        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<byte> input)
        {
            var dict = new OccurenceDictionary<byte>();
            await foreach (var symbol in input)
            {
                dict.Add(symbol);
                yield return symbol;
            }

            double sum = dict.Sum();
            double entropy = 0;
            foreach (var freq in dict.Values)
                entropy -= (freq / sum) * Math.Log2(freq / sum);
            Console.WriteLine($"[Entropy] Entropy: {entropy}; Length: {(int) sum}; Symobls: {dict.Count}");
            Console.WriteLine($"[Entropy] Predicted size: {entropy * sum / 8}");
        }

        public async IAsyncEnumerable<ReadOnlyMemory<ushort>> Encode(IAsyncEnumerable<ReadOnlyMemory<ushort>> input)
        {
            await foreach (var block in input)
            {
                var dict = new OccurenceDictionary<ushort>();
                for (int i = 0; i < block.Length; i++)
                    dict.Add(block.Span[i]);

                double sum = dict.Sum();
                double entropy = 0;
                foreach (var freq in dict.Values)
                    entropy -= (freq / sum) * Math.Log2(freq / sum);
                Console.WriteLine($"[Entropy] Entropy: {entropy}; Length: {block.Length}; Symbols: {dict.Count}");
                Console.WriteLine($"[Entropy] Predicted size: {entropy * block.Length / 8}");

                yield return block;
            }
        }

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> Encode(IAsyncEnumerable<ReadOnlyMemory<byte>> input)
        {
            await foreach (var block in input)
            {
                var dict = new OccurenceDictionary<byte>();
                for (int i = 0; i < block.Length; i++)
                    dict.Add(block.Span[i]);

                double sum = dict.Sum();
                double entropy = 0;
                foreach (var freq in dict.Values)
                    entropy -= (freq / sum) * Math.Log2(freq / sum);
                Console.WriteLine($"[Entropy] Entropy: {entropy}; Length: {block.Length}; Symbols: {dict.Count}");
                Console.WriteLine($"[Entropy] Predicted size: {entropy * sum / 8}");

                yield return block;
            }
        }
    }
}