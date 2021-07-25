using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Algorithms.BWD.Matching;
using BWDPerf.Transforms.Algorithms.BWD.Ranking;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class BWDTests
    {
        private const string _enwik4 = "../../../../data/enwik4";
        private const string _dictFile = "../../../dict";
        private const string _compressedFile = "../../../compressed";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task EntropyRankingLCPMatchfinderTest() =>
            await RunBWDWithOptions(new EntropyRanking(), new LCPMatchFinder(), Guid.NewGuid());

        [TestMethod]
        public async Task NaiveRankingLCPMatchfinderTest() =>
            await RunBWDWithOptions(new NaiveRanking(maxWordSize: 150), new LCPMatchFinder(), Guid.NewGuid());

        [TestMethod]
        public async Task EntropyRankingLCPStaticMatchfinderTest() =>
            await RunBWDWithOptions(new EntropyRanking(), new LCPStaticMatchFinder(), Guid.NewGuid());

        private static async Task RunBWDWithOptions(IBWDRankProvider ranking, IBWDMatchProvider matching, Guid guid)
        {
            // Compute dictionary
            await new BufferedFileSource(_enwik4, 100_000)
                .ToCoder(new BWD(ranking, matching))
                .ToCoder(new BWDictionaryEncoder())
                .Serialize(new SerializeToFile(_dictFile + guid));

            // Read dictionary
            var dictionary = await new FileSource(_dictFile + guid)
                .ToDecoder(new BWDictionaryDecoder())
                .First();

            // Compress
            await new BufferedFileSource(_enwik4, 100_000)
                .ToCoder(new BWDParser(dictionary))
                .Serialize(new SerializeToFile(_compressedFile + guid));

            // Decompress
            await new BWDFileSource(_compressedFile + guid)
                .ToDecoder(new BWDDecoder(dictionary))
                .Serialize(new SerializeToFile(_decompressedFile + guid));

            Assert.IsTrue(CompareFiles(_enwik4,_decompressedFile + guid));
        }

        private static bool CompareFiles(string filePath1, string filePath2)
            => File.ReadAllBytes(filePath1).SequenceEqual(File.ReadAllBytes(filePath2));
    }
}