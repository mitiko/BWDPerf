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

var task = new BufferedFileSource(args[0], 1 << 20, useProgressBar: false) // 1MB
    .ToCoder<byte[], byte[]>(new CapitalConversion())
    .ToCoder<byte[], DictionaryIndex>(new BWD(indexSize: 8, maxSizeWord: 24, decideToEnd: false))
    .Serialize(new DiscardSerializer<DictionaryIndex>());

await task;
Console.WriteLine($"Elapsed: {timer.Elapsed}");
