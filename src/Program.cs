using System;
using BWDPerf.Architecture;
using BWDPerf.Common.Tools;
using BWDPerf.Common.Serializers;
using BWDPerf.Common.Sources;

Console.WriteLine("Started");

if (args.Length != 1)
    args = new string[] { ".data/file.md" };

Console.WriteLine("------------No Cap------------");
var task = new FileSource(args[0])
    .ToCoder(new MeasureEntropy())
    // .Serialize(new DiscardSerializer());
    .Serialize(new SerializeToFile(".data/conv1.md"));

await task;

// Console.WriteLine("\n\n-------------Cap--------------");
// var taskCap = new FileSource(args[0])
//     .ToCoder(new CapitalConversion())
//     .ToCoder(new MeasureEntropy())
//     .Serialize(new SerializeToFile(".data/conv2"));

// await taskCap;
