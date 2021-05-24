using System;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Algorithms.BWD.Matching;
using BWDPerf.Transforms.Algorithms.BWD.Entities;
using BWDPerf.Transforms.Algorithms.BWD.Ranking;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS;
using BWDPerf.Transforms.Modeling.Alphabets;
using BWDPerf.Transforms.Modeling.Mixers;
using BWDPerf.Transforms.Modeling.Submodels;
using BWDPerf.Transforms.Modeling.Quantizers;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;
using System.IO;
using System.Linq;

class Program
{
    static string _file = "/home/mitiko/Documents/Projects/Compression/BWDPerf/data/calgary/book1";
    static async Task Main(string[] args)
    {
        var _file = "/home/mitiko/Documents/Projects/Compression/BWDPerf/data/calgary/book1";
        var timer = System.Diagnostics.Stopwatch.StartNew();

        // Compresion
        Console.WriteLine("Compressing...");
        await Compress(loadDictionary: true);
        Console.WriteLine($"Compression took: {timer.Elapsed}"); timer.Restart();

        // Output stats
        var fileName = _file.Split('/').Last();
        var fileSize = new FileInfo(_file).Length;
        var compressedSize = new FileInfo("encoded.bwd").Length + new FileInfo("dict.bwd").Length;
        Console.WriteLine($"{fileName}: {fileSize} -> {compressedSize}; ratio: {compressedSize * 1d / fileSize}");

        // Decompression
        Console.WriteLine("Decompressing...");
        await Decompress();
        Console.WriteLine($"Decompression took: {timer.Elapsed}"); timer.Stop();

        // Check if decompression was successful
        var correctDecode = File.ReadAllBytes(_file).SequenceEqual(File.ReadAllBytes("decoded"));
        Console.WriteLine($"Correct decode: {correctDecode}");
    }

    private static async Task Compress(bool loadDictionary = false)
    {
        BWDictionary dict;
        if (loadDictionary)
        {
            // Load the dictionary
            dict = await new FileSource("dict.bwd")
                .ToDecoder(new BWDictionaryDecoder())
                .First();
        }
        else
        {
            // Compute the dictionary
            dict = await new BufferedFileSource(_file, 1_000_000)
                .ToCoder(new BWD(new EntropyRanking(), new LCPMatchFinder()))
                .First();

            // Save the dictionary
            await dict.AsAsyncEnumerable()
                .ToCoder(new BWDictionaryEncoder())
                .Serialize(new SerializeToFile("dict.bwd"));
        }

        // Create the model
        var alphabet = new DictionaryAlphabet(dict.Keys.ToArray());
        var order0 = new Order0(alphabet.Length);
        var quantizer = new BasicQuantizer(order0);

        // Compress
        await new BufferedFileSource(_file, 1_000_000)
            .ToCoder(new BWDParser(dict))
            .ToCoder<ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>>(new MeasureEntropy())
            .ToCoder(new RANSEncoder<ushort>(alphabet, quantizer))
            .Serialize(new SerializeToFile("encoded.bwd"));
    }

    private static async Task Decompress()
    {
        // Load the dictionary
        var dict = await new FileSource("dict.bwd")
            .ToDecoder(new BWDictionaryDecoder())
            .First();

        // Create the model
        var alphabet = new DictionaryAlphabet(dict.Keys.ToArray());
        var order0 = new Order0(alphabet.Length);
        var quantizer = new BasicQuantizer(order0);

        // Decompress
        await new FileSource("encoded.bwd")
            .ToDecoder(new RANSDecoder<ushort>(alphabet, quantizer))
            .ToDecoder(new BWDDecoder(dict))
            .Serialize(new SerializeToFile("decoded"));
    }
}