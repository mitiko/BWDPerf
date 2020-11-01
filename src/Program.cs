using System;
using BWDPerf.Architecture;
using BWDPerf.Common.Tools;
using BWDPerf.Common.Serializers;
using BWDPerf.Common.Sources;
using System.Diagnostics;
using BWDPerf.Common.Entities;
using BWDPerf.Common.Algorithms.BWD;

Console.WriteLine("Started");

if (args.Length != 1)
    args = new string[] { "../data/file.md" };

var timer = Stopwatch.StartNew();

var task = new BufferedFileSource(args[0], 16 << 10, useProgressBar: false) // 16KB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToCoder<byte[], DictionaryIndex>(new BWD(16, 8, 8))
    .ToCoder<DictionaryIndex, byte>(new MeasureEntropy())
    .Serialize(new DiscardSerializer<byte>());

await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");
