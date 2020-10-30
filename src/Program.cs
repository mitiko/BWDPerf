using System;
using BWDPerf.Architecture;
using BWDPerf.Common.Tools;
using BWDPerf.Common.Serializers;
using BWDPerf.Common.Sources;
using System.Diagnostics;

Console.WriteLine("Started");

if (args.Length != 1)
    args = new string[] { "../data/file.md" };

var timer = Stopwatch.StartNew();
var task = new FileSource(args[0])
    .ToCoder(new MeasureEntropy())
    .ToCoder(new CapitalConversion())
    .ToCoder(new CountWord("the"))
    .ToCoder(new MeasureEntropy())
    .Serialize(new SerializeToFile("../data/conv1.md"));

await task;
System.Console.WriteLine($"Elapsed: {timer.Elapsed}");