using System;
using BWDPerf.Architecture;
using BWDPerf.Common.Tools;
using BWDPerf.Common.Serializers;
using BWDPerf.Common.Sources;
using System.Diagnostics;
using BWDPerf.Common.Entities;
using BWDPerf.Common.Algorithms.BWD;
using BWDPerf.Interfaces;
using System.Collections.Generic;

Console.WriteLine("Started");

if (args.Length != 1)
    args = new string[] { "../data/enwik4" };

var timer = Stopwatch.StartNew();

var task = new BufferedFileSource(args[0], 10_000_000, useProgressBar: false) // 10MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToDualOutputCoder(new BWDEncoder(new Options(indexSize: 5, maxWordSize: 16)))
    .ToCoder(new DictionaryToBytes())
    // .ToCoder(new MeasureEntropy())
    .Serialize(new SerializeToFile("enwik4.bwd"));


await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");

public class DictionaryToBytes : ICoder<(byte[], DictionaryIndex[]), byte>
{
    public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<(byte[], DictionaryIndex[])> input)
    {
        await foreach (var (dictionary, stream) in input)
        {
            // Write out the dictionary
            foreach (var @byte in dictionary)
                yield return @byte;

            var bits = new Queue<bool>();
            foreach (var index in stream)
            {
                // Write bits of the current index to the queue
                for (int i = index.BitsToUse - 1; i >= 0; i--)
                    bits.Enqueue((index.Index & (1 << i)) != 0);

                // Flush out buffered bytes if any
                while (bits.Count >= 8) yield return ReadFromBitBuffer();
            }

            // If we still have bits, write the rest and pad them with zeroes
            if (bits.Count > 0) yield return ReadFromBitBuffer();

            byte ReadFromBitBuffer()
            {
                byte n = 0;
                for (int i = 0; i < 8; i++)
                {
                    n <<= 1;
                    if (bits.TryDequeue(out bool checkBit))
                        if (checkBit) n += 1;
                }
                return n;
            }
        }
    }
}