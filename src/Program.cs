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
using System.Linq;

Console.WriteLine("Started");

if (args.Length != 1)
    args = new string[] { "../data/enwik4" };

var file = new System.IO.FileInfo(args[0]);

var timer = Stopwatch.StartNew();

var task = new BufferedFileSource(args[0], 10_000_000, useProgressBar: false) // 10MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToDualOutputCoder(new BWDEncoder(new Options(indexSize: 6, maxWordSize: 12, bpc: 8)))
    .ToCoder(new CalcEntropy())
    .ToCoder(new DictionaryToBytes())
    .Serialize(new SerializeToFile($"{file.Name}.bwd"));


await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");

public class CalcEntropy : ICoder<(byte[], DictionaryIndex[]), (byte[], DictionaryIndex[])>
{
    public async IAsyncEnumerable<(byte[], DictionaryIndex[])> Encode(IAsyncEnumerable<(byte[], DictionaryIndex[])> input)
    {
        await foreach (var (dictionary, stream) in input)
        {
            var count = stream.Length;
            var countd = dictionary.Length;
            var od = new BWDPerf.Tools.OccurenceDictionary<int>();
            foreach (var symbol in stream)
                od.Add(symbol.Index);
            var odd = new BWDPerf.Tools.OccurenceDictionary<byte>();
            foreach (var symbol in dictionary)
                odd.Add(symbol);

            double total = od.Sum();
            var entropy = od.Values
                .Select(x => x / total)
                .Select(x => - Math.Log2(x) * x)
                .Sum();

            double totald = odd.Sum();
            var entropyd = odd.Values
                .Select(x => x / totald)
                .Select(x => - Math.Log2(x) * x)
                .Sum();

            Console.WriteLine($"Calculated entropy or something: e={entropy}; c={count}; space={entropy * count / 8}");
            Console.WriteLine($"Dictionary entropy: e={entropyd}; c={countd}; space={entropyd * countd / 8}");
            Console.WriteLine($"Total space: {entropy * count / 8 + entropyd * countd / 8} no dict => {entropy * count / 8 + countd}");

            yield return (dictionary, stream);
        }
    }
}