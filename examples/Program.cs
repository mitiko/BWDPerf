﻿using System;
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
using BWDPerf.Transforms.Algorithms.EntropyCoders.StaticRANS;
using BWDPerf.Transforms.Modeling;
using BWDPerf.Transforms.Modeling.Submodels;
using BWDPerf.Transforms.Quantizers;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;


class Program
{
    static async Task Main(string[] args)
    {
        var alphabet = Alphabet<byte>.ForText();
        var model = new Order0(alphabet.Length);
        var quantizer = new BasicQuantizer(model);
        Console.WriteLine("Initialized");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var compressTask = new BufferedFileSource("/home/mitiko/Documents/Projects/Compression/BWDPerf/data/file.md", 10_000_000) // 10MB
                .ToCoder(new RANSEncoder<byte>(alphabet, quantizer))
                .Serialize(new SerializeToFile("encoded.rans"));
        await compressTask;
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        // await new BWDBenchmark().Compress();
        Console.WriteLine($"Compression took: {timer.Elapsed}");
        timer.Restart();
        // await new BWDBenchmark().Decompress();
        var alphabet1 = Alphabet<byte>.ForText();
        var model1 = new Order0(alphabet1.Length);
        var quantizer1 = new BasicQuantizer(model1);
        var decompressTask = new FileSource("encoded.rans")
            .ToDecoder(new RANSDecoder<byte>(alphabet1, quantizer1))
            .Serialize(new SerializeToFile("decoded.rans"));

        await decompressTask;
        Console.WriteLine($"Decompression took: {timer.Elapsed}");

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