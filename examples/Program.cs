using System;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Algorithms.BWD.Matching;
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
using BWDPerf.Transforms.Converters;

class Program
{
    static readonly string _file = @"/home/mitiko/Documents/Projects/Compression/BWDPerf/data/calgary/book1";
    // static readonly string _file = @"/home/mitiko/Documents/Projects/Compression/BWDPerf/data/book11";
    static async Task Main()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();

        // Compresion
        Console.WriteLine("Compressing...");
        await ComputeDict(loadDictionary: true);
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
        // var order0 = new Order0(alphabet.Length);
        // var order1 = new Order1(alphabet.Length);
        var order2 = new ByteOrder2(alphabet.Length);
        var model = order2;
        var quantizer = new BasicQuantizer();
        var converter = new NibbleBlockConverter();

        // Load dictionary
        var dict = await new FileSource("dict_book1")
            .ToDecoder(new BWDictionaryDecoder())
            .First();

        // Compress
        await new BufferedFileSource(_file, 1_000_000)
            .ToCoder(new BWDParser(dict))
            .ToCoder<ReadOnlyMemory<ushort>, ReadOnlyMemory<ushort>>(new MeasureEntropy())
            .ToCoder(new RANSEncoder<ushort, byte>(alphabet, model, quantizer, converter))
            // .ToCoder(new RANSEncoder<byte>(alphabet, quantizer))
            .Serialize(new SerializeToFile("encoded.nb"));
    }

    private static async Task Decompress()
    {
        // Create the model
        // var alphabet = new TextAlphabet();
        var alphabet = new NibbleAlphabet();
        // var order1 = new Order1(alphabet.Length);
        var order2 = new ByteOrder2(alphabet.Length);
        var model = order2;
        var quantizer = new BasicQuantizer();
        var converter = new NibbleConverter();

        // Load dictionary
        var dict = await new FileSource("dict_book1")
            .ToDecoder(new BWDictionaryDecoder())
            .First();

        // Decompress
        await new FileSource("encoded.nb")
            .ToDecoder(new RANSDecoder<byte, ushort>(alphabet, model, quantizer, converter))
            .ToDecoder(new BWDDecoder(dict))
            .Serialize(new SerializeToFile("decoded"));
    }

    private static async Task ComputeDict(bool loadDictionary)
    {
        if (loadDictionary) return;
        await new BufferedFileSource(_file, 1_000_000)
            .ToCoder(new BWD(new EntropyRanking(), new LCPStaticMatchFinder()))
            .ToCoder(new BWDictionaryEncoder())
            .Serialize(new SerializeToFile("dict_book1"));
    }
}