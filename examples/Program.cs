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
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;


class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Started");
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        await new BWDBenchmark().Compress();
        await new BWDBenchmark().Decompress();
    }
}

public class BWDBenchmark
{
    [Benchmark]
    public async Task Compress()
    {
        var encodeTask = new BufferedFileSource("/home/mitiko/Documents/Projects/Compression/BWDPerf/data/enwik4", 1_000)
            .ToCoder(new BWDEncoder(new Options(maxWordSize: 12, indexSize: 5), new NaiveRanking(8, 5, 12)))
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