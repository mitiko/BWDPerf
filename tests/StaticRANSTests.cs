using System.IO;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.EntropyCoders.StaticRANS;
using BWDPerf.Transforms.Converters;
using BWDPerf.Transforms.Models.Static.RANS;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class StaticRANSTests
    {
        private const string _file = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.rans";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task TestCompression()
        {
            var compressTask = new BufferedFileSource(_file, 10_000_000) // 10MB
                .ToCoder(new StaticRANSEncoder<byte>(new StaticOrder0<byte>(), new ByteConverter()))
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;
        }

        [TestMethod]
        public async Task TestDecompression()
        {
            var decompressTask = new FileSource(_compressedFile)
                .ToDecoder(new StaticRANSDecoder<byte>(new StaticOrder0<byte>(), new ByteConverter()))
                .Serialize(new SerializeToFile(_decompressedFile));

            await decompressTask;
        }

        [TestMethod]
        public void TestIntegrity()
        {
            byte[] original = File.ReadAllBytes(_file);
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