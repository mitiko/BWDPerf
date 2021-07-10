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
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANSNibbled;

class Program
{
    // static readonly string _file = @"C:\Users\HP\Documents\BWDPerf\data\calgary\book1";
    static readonly string _file = @"C:\Users\HP\Documents\BWDPerf\data\book11";
    static async Task Main()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();

        // Compresion
        Console.WriteLine("Compressing...");
        await Compress();
        Console.WriteLine($"Compression took: {timer.Elapsed}"); timer.Restart();

        // Output stats
        var fileName = _file.Split('/').Last();
        var fileSize = new FileInfo(_file).Length;
        var compressedSize = new FileInfo("encoded.nb").Length;
        Console.WriteLine($"{fileName}: {fileSize} -> {compressedSize}; ratio: {compressedSize * 1d / fileSize}");

        // Decompression
        Console.WriteLine("Decompressing...");
        await Decompress();
        Console.WriteLine($"Decompression took: {timer.Elapsed}"); timer.Stop();

        // Check if decompression was successful
        var correctDecode = File.ReadAllBytes(_file).SequenceEqual(File.ReadAllBytes("decoded"));
        Console.WriteLine($"Correct decode: {correctDecode}");
    }

    private static async Task Compress()
    {
        // Create the model
        // var alphabet = new TextAlphabet();
        var alphabet = new NibbleAlphabet();
        var order1 = new Order1(alphabet.Length);
        var quantizer = new BasicQuantizer(order1);

        // Compress
        await new BufferedFileSource(_file, 1_000_000)
            .ToCoder<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(new MeasureEntropy())
            .ToCoder(new RANSNibbledEncoder<byte>(alphabet, quantizer))
            // .ToCoder(new RANSEncoder<byte>(alphabet, quantizer))
            .Serialize(new SerializeToFile("encoded.nb"));
    }

    private static async Task Decompress()
    {
        // Create the model
        // var alphabet = new TextAlphabet();
        var alphabet = new NibbleAlphabet();
        var order1 = new Order1(alphabet.Length);
        var quantizer = new BasicQuantizer(order1);

        // Decompress
        await new FileSource("encoded.nb")
            .ToDecoder(new RANSNibbledDecoder<byte>(alphabet, quantizer))
            // .ToDecoder(new RANSDecoder<byte>(alphabet, quantizer))
            .Serialize(new SerializeToFile("decoded"));
    }
}