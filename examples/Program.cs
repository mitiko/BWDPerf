using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
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
    }
}

public class BWDBenchmark
{
    [Benchmark]
    public async Task Compress()
    {
        var encodeTask = new BufferedFileSource("/home/mitiko/Documents/Projects/Compression/BWDPerf/data/enwik7", 10_000_000)
            .ToCoder(new BWDEncoder(new Options(maxWordSize: 32, indexSize: 8), new NaiveRanking(8, 8)))
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