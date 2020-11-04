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

var task = new BufferedFileSource(args[0], 1 << 20, useProgressBar: false) // 1MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToCoder(new BWD(new Options(indexSize: 9, maxWordSize: 24, autoEnd: false)))
    .ToCoder(new Unbuffer<DictionaryIndex>())
    .ToCoder(new DictionaryToBytes())
    .ToCoder(new MeasureEntropy())
    .Serialize(new DiscardSerializer<byte>());


await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");

public class DictionaryToBytes : ICoder<DictionaryIndex, byte>
{
    public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<DictionaryIndex> input)
    {
        await foreach (var item in input)
        {
            for (int i = 0; i < item.BitsToUse; i++)
            {
                yield return (byte) item.Index;
            }
        }
    }
}