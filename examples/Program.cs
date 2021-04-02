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
using BWDPerf.Transforms.Algorithms.BWD.Matching;
using BWDPerf.Transforms.Algorithms.BWD.Entities;
using BWDPerf.Transforms.Algorithms.BWD.Ranking;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;


class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Started");
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        await new BWDBenchmark().Compress();
        Console.WriteLine($"Compression took: {timer.Elapsed}");
        timer.Restart();
        await new BWDBenchmark().Decompress();
        Console.WriteLine($"Decompression took: {timer.Elapsed}");

    }
}

public class BWDBenchmark
{
    [Benchmark]
    public async Task Compress()
    {
        // var ranking = new Order1EntropyRanking();
        // var ranking = new EntropyRanking();
        var ranking = new NaiveRanking(8, 12);
        var matching = new LCPMatchFinder(12);
        var encodeTask = new BufferedFileSource("/home/mitiko/Documents/Projects/Compression/BWDPerf/data/book11", 100_000_000)
            // .ToCoder(new CapitalConversion())
            .ToCoder(new BWDEncoder(ranking, matching))
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