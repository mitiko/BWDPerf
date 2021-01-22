using System.IO;
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
    public class BWDStandaloneTest
    {
        private const string _enwik4 = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.bwd";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task TestCompression()
        {
            var compressTask = new BufferedFileSource(_enwik4, 10_000_000) // 10MB
                .ToCoder(new BWDEncoder(new Options(indexSize: 8, maxWordSize: 12, bpc: 8)))
                .ToCoder(new DictionaryToBytes())
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;
        }

        [TestMethod]
        public async Task TestDecompression()
        {
            var decompressTask = new FileSource(_compressedFile)
                .ToDecoder<byte, byte[]>(new BWDDecoder())
                .Serialize(new SerializeToFile(_decompressedFile));

            await decompressTask;
        }

        [TestMethod]
        public void TestIntegrity()
        {
            byte[] original = File.ReadAllBytes(_enwik4);
            byte[] decompressed = File.ReadAllBytes(_decompressedFile);
            if (original.Length == decompressed.Length)
            {
                for (int i = 0; i < original.Length; i++)
                {
                    if (original[i] != decompressed[i])
                        Assert.Fail($"Files differ at byte {i}.");
                }
            }
            else Assert.Fail("Files are of different length.");
        }
    }
}
