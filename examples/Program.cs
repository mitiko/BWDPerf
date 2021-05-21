using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BWDPerf.Architecture;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Algorithms.BWD.Entities;
using BWDPerf.Transforms.Algorithms.BWD.Ranking;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS;
using BWDPerf.Transforms.Modeling;
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
    static async Task Main(string[] args)
    {
        var _file = "/home/mitiko/Documents/Projects/Compression/BWDPerf/data/calgary/book1";
        var alphabet = new TextAlphabet();
        var timer = System.Diagnostics.Stopwatch.StartNew();

        // Compression
        Console.WriteLine("Compressing...");
        var modelA = new Order0(alphabet.Length);
        var modelB = new Order1(alphabet.Length);
        var modelC = new ByteOrder2(alphabet.Length);
        var modelD = new ByteOrder3(alphabet.Length);
        var modelE = new ByteOrder4(alphabet.Length);
        var model = new Mixer(lr: 0.15, n: alphabet.Length, modelE, modelD, modelC, modelB, modelA);
        var quantizer = new BasicQuantizer(model);
        var compressTask = new BufferedFileSource(_file, 1_000_000) // 1MB
                .ToCoder(new RANSEncoder<byte>(alphabet, quantizer))
                .Serialize(new SerializeToFile("encoded.rans"));
        await compressTask;

        Console.WriteLine($"Compression took: {timer.Elapsed}");
        timer.Restart();

        var ratio = (new FileInfo("encoded.rans").Length) * 1d / (new FileInfo(_file).Length);
        Console.WriteLine($"Compression ratio: {ratio}");
        var fileName = _file.Split('/').Last();
        Console.WriteLine($"{fileName} -> {new FileInfo("encoded.rans").Length}");

        // Decompression
        Console.WriteLine("Decompressing...");
        var modelA1 = new Order0(alphabet.Length);
        var modelB1 = new Order1(alphabet.Length);
        var modelC1 = new ByteOrder2(alphabet.Length);
        var modelD1 = new ByteOrder3(alphabet.Length);
        var modelE1 = new ByteOrder4(alphabet.Length);
        var model1 = new Mixer(lr: 0.15, n: alphabet.Length, modelE1, modelD, modelC1, modelB1, modelA1);
        var quantizer1 = new BasicQuantizer(model1);
        var decompressTask = new FileSource("encoded.rans")
            .ToDecoder(new RANSDecoder<byte>(alphabet, quantizer1))
            .Serialize(new SerializeToFile("decoded.rans"));

        await decompressTask;
        Console.WriteLine($"Decompression took: {timer.Elapsed}");

        // Write some stats
        var correctDecode = File.ReadAllBytes(_file).SequenceEqual(File.ReadAllBytes("decoded.rans"));
        Console.WriteLine($"Correct decode: {correctDecode}");
    }
}

public class BWDBenchmark
{
    [Benchmark]
    public async Task Compress()
    {
        var options = new Options(maxWordSize: 12);
        var ranking = new EntropyRanking();
        // var ranking = new NaiveRanking(options);
        var encodeTask = new BufferedFileSource("/home/mitiko/Documents/Projects/Compression/BWDPerf/data/enwik4", 100_000_000)
            // .ToCoder(new CapitalConversion())
            .ToCoder(new BWDEncoder(options, ranking))
            .ToCoder<BWDBlock, BWDBlock>(new MeasureEntropy())
            .ToCoder(new BlockToBytes())
            .Serialize(new SerializeToFile("encoded"));

        await encodeTask;
    }

    [Benchmark]
    public async Task Decompress()
    {
        var decodeTask = new FileSource("encoded")
            .ToDecoder(new BWDRawDecoder())
            .ToDecoder(new BlockToBytes())
            .Serialize(new SerializeToFile("decoded"));

        await decodeTask;
    }
}