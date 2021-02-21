using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.BWD;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class BWDStandaloneTests
    {
        private const string _enwik4 = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.bwd";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task ByteAlignedOneDictionary() =>
            await RunBWDWithOptions(new Options(indexSize: 8, maxWordSize: 12), 100_000); // 100KB

        [TestMethod]
        public async Task NotByteAlignedOneDictionary() =>
            await RunBWDWithOptions(new Options(indexSize: 6, maxWordSize: 12), 100_000); // 100KB

        [TestMethod]
        public async Task ByteAlignedMultipleDictionaries() =>
            await RunBWDWithOptions(new Options(indexSize: 8, maxWordSize: 12), 1_000); // 1KB

        [TestMethod]
        public async Task NotByteAlignedMultipleDictionaries() =>
            await RunBWDWithOptions(new Options(indexSize: 5, maxWordSize: 12), 1_000); // 1KB

        private async Task RunBWDWithOptions(Options options, int bufferSize)
        {
            var compressTask = new BufferedFileSource(_enwik4, bufferSize)
                .ToCoder(new BWDEncoder(options))
                .ToCoder(new BlockToBytes())
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;

            var decompressTask = new FileSource(_compressedFile)
                .ToDecoder(new BWDRawDecoder())
                .ToDecoder(new BlockToBytes())
                .Serialize(new SerializeToFile(_decompressedFile));

            await decompressTask;

            Assert.AreEqual(ComputeHash(_enwik4), ComputeHash(_decompressedFile));
        }

        private string ComputeHash(string filePath)
        {
            using var sha256 = SHA256Managed.Create();
            using var fileStream = File.OpenRead(filePath);
            return Convert.ToBase64String(sha256.ComputeHash(fileStream));
        }
    }
}