using System;
using System.Diagnostics;
using BWDPerf.Architecture;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;

var source = new BufferedFileSource("../data/enwik4", 10_000_000);
var compressedSource = new FileSource("encoded");
var bwdEncoder = new BWDEncoder(new Options(maxWordSize: 16, indexSize: 8));
var bwdDecoder = new BWDDecoder();
var bwdToBytes = new DictionaryToBytes();
var c_serializer = new SerializeToFile("encoded");
var d_serializer = new SerializeToFile("decoded");

Console.WriteLine("Started");
var timer = Stopwatch.StartNew();

var encodeTask = source
    .ToCoder(bwdEncoder)
    .ToCoder(bwdToBytes)
    .Serialize(c_serializer);

await encodeTask;
Console.WriteLine($"Compression took: {timer.Elapsed}");
timer.Restart();

var decodeTask = compressedSource
    .ToDecoder(bwdDecoder)
    .Serialize(d_serializer);

await decodeTask;
Console.WriteLine($"Decompression took: {timer.Elapsed}");


var data = new byte[]
{
    (byte) 'm',
    (byte) 'i',
    (byte) 's',
    (byte) 's',
    (byte) 'i',
    (byte) 's',
    (byte) 's',
    (byte) 'i',
    (byte) 'p',
    (byte) 'p',
    (byte) 'i'
};
var sa = new SuffixArray(data);
sa.Print();
