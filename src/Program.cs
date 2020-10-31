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
    args = new string[] { "../data/short.md" };

var timer = Stopwatch.StartNew();
var task = new BufferedFileSource(args[0])
    .ToCoder<byte[], DictionaryIndex>(new BWD())
    .Serialize(new DiscardSerializer<DictionaryIndex>());

await task;
System.Console.WriteLine($"Elapsed: {timer.Elapsed}");