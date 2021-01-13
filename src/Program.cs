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
    args = new string[] { "../data/enwik6" };

var timer = Stopwatch.StartNew();

var task = new BufferedFileSource(args[0], 10_000_000, useProgressBar: false) // 10MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToCoder(new BWD(new Options(indexSize: 6, maxWordSize: 12)))
    .ToCoder(new Unbuffer<DictionaryIndex>())
    .ToCoder(new DictionaryToBytes())
    .ToCoder(new MeasureEntropy())
    .Serialize(new SerializeToFile("enwik6.bwd"));


await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");

public class DictionaryToBytes : ICoder<DictionaryIndex, byte>
{
    public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<DictionaryIndex> input)
    {
        await foreach (var item in input)
        {
            yield return (byte) item.Index;
        }
    }
}