using System;
using System.Diagnostics;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;

// var source = new BufferedFileSource("../data/enwik4", 10_000_000);
var source = new BufferedFileSource("../data/enwik4", 1_000);
var compressedSource = new FileSource("encoded");
var bwdEncoder = new BWDEncoder(new Options(maxWordSize: 8, indexSize: 7));
var bwdDecoder = new BWDDecoder();
var bwdToBytes = new DictionaryToBytes();
var c_serializer = new SerializeToFile("encoded");
var d_serializer = new SerializeToFile("decoded");

// Console.WriteLine("Started");
// var timer = Stopwatch.StartNew();

// var encodeTask = source
//     .ToCoder(bwdEncoder)
//     .ToCoder(bwdToBytes)
//     .Serialize(c_serializer);

// await encodeTask;
// Console.WriteLine($"Compression took: {timer.Elapsed}");
// timer.Restart();

// var decodeTask = compressedSource
//     .ToDecoder(bwdDecoder)
//     .Serialize(d_serializer);

// await decodeTask;
// Console.WriteLine($"Decompression took: {timer.Elapsed}");

// var en = source.Fetch().GetAsyncEnumerator();
// for (int i = 0; i < 7; i++)
//     await en.MoveNextAsync();

// var sa = new BWDPerf.Tools.SuffixArray(en.Current);
// var word = en.Current.Slice(641, 2);
// Console.WriteLine("Word:");
// WriteWord(word);
// // WriteWord(en.Current.Slice(2551, 2));
// Console.WriteLine("Suffix array found matches at: ");
// foreach (var match in sa.Search(en.Current, word))
// {
//     Console.WriteLine(match);
// }

// Console.WriteLine("CHECKING WORDS:");
// WriteWord(en.Current.Slice(0, 20));
// WriteWord(en.Current.Slice(72, 2));
// WriteWord(en.Current.Slice(627, 2));
// WriteWord(en.Current.Slice(856, 2));
// Console.WriteLine("Matches -------");
// WriteWord(en.Current.Slice(662, 3));
// WriteWord(en.Current.Slice(413, 3));
// Console.WriteLine("---------------");
// WriteWord(en.Current.Slice(1022, 2));
// WriteWord(en.Current.Slice(641, 3));
// WriteWord(en.Current.Slice(994, 2));

// void WriteWord(ReadOnlyMemory<byte> word)
// {
//     Console.Write("\"");
//     for (int i = 0; i < word.Length; i++)
//     {
//         Console.Write((char) word.Span[i]);
//     }
//     Console.WriteLine("\"");
// }