using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Algorithms.BWD.Matching;
using BWDPerf.Transforms.Algorithms.BWD.Ranking;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class BWDNaiveRankingStandaloneTests
    {
        private const string _enwik4 = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.bwd";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task OneDictionary() =>
            await RunBWDWithOptions(bufferSize: 100_000); // 100KB

        [TestMethod]
        public async Task MultipleDictionaries() =>
            await RunBWDWithOptions(bufferSize: 1_000); // 1KB

        private async Task RunBWDWithOptions(int bufferSize)
        {
            var ranking = new NaiveRanking(150);
            var matching = new LCPMatchFinder();
            var compressTask = new BufferedFileSource(_enwik4, bufferSize)
                .ToCoder(new BWDEncoder(ranking, matching))
                .ToCoder(new BlockToBytes())
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;

            var decompressTask = new FileSource(_compressedFile)
                .ToDecoder(new BWDRawDecoder())
                .ToDecoder(new BlockToBytes())
                .Serialize(new SerializeToFile(_decompressedFile));

            await decompressTask;

            Assert.IsTrue(CompareFiles(_enwik4,_decompressedFile));
        }

        private bool CompareFiles(string filePath1, string filePath2)
            => File.ReadAllBytes(filePath1).SequenceEqual(File.ReadAllBytes(filePath2));
    }
}