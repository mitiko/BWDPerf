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

var timer = Stopwatch.StartNew();

var task = new BufferedFileSource(args[0], 10_000_000, useProgressBar: false) // 10MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToDualOutputCoder(new BWDEncoder(new Options(indexSize: 9, maxWordSize: 24)))
    .ToCoder(new CalcEntropy())
    .ToCoder(new DictionaryToBytes())
    .Serialize(new SerializeToFile("enwik4.bwd"));


await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");

public class CalcEntropy : ICoder<(byte[], DictionaryIndex[]), (byte[], DictionaryIndex[])>
{
    public async IAsyncEnumerable<(byte[], DictionaryIndex[])> Encode(IAsyncEnumerable<(byte[], DictionaryIndex[])> input)
    {
        await foreach (var (dictionary, stream) in input)
        {
            var count = stream.Length;
            var od = new BWDPerf.Tools.OccurenceDictionary<byte>();
            foreach (var symbol in stream)
                od.Add((byte) symbol.Index);

            double total = od.Sum();
            var entropy = od.Values
                .Select(x => x / total)
                .Select(x => - Math.Log2(x) * x)
                .Sum();

            Console.WriteLine($"Calculated entropy or something: e={entropy}; c={count}; d={dictionary.Length} space={entropy * count / 8 + dictionary.Length}");

            yield return (dictionary, stream);
        }
    }
}