using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS;
using BWDPerf.Transforms.Modeling.Alphabets;
using BWDPerf.Transforms.Modeling.Submodels;
using BWDPerf.Transforms.Modeling.Quantizers;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BWDPerf.Transforms.Converters;
using System;

namespace BWDPerf.Tests
{
    [TestClass]
    public class RANSTests
    {
        private const string _file = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.rans";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task RANSBytewiseTest()
        {
            var guid = Guid.NewGuid();
            var alphabet = new TextAlphabet();
            var model = new Order0(alphabet.Length);

            // Compress
            await new BufferedFileSource(_file, 10_000_000) // 10MB
                .ToCoder(new RANSEncoder<byte, byte>(alphabet, model, new BasicQuantizer(), new IdentityBlockConverter()))
                .Serialize(new SerializeToFile(_compressedFile + guid));

            // Decompress
            model = new Order0(alphabet.Length);
            await new FileSource(_compressedFile + guid)
                .ToDecoder(new RANSDecoder<byte, byte>(alphabet, model, new BasicQuantizer(), new IdentityConverter()))
                .Serialize(new SerializeToFile(_decompressedFile + guid));

            TestIntegrity(_decompressedFile + guid, _file);
        }

        [TestMethod]
        public async Task RANSNibblewiseTest()
        {
            var guid = Guid.NewGuid();
            var alphabet = new NibbleAlphabet();
            var model = new ByteOrder2(alphabet.Length);

            // Compress
            await new BufferedFileSource(_file, 10_000_000) // 10MB
                .ToCoder(new RANSEncoder<byte, byte>(alphabet, model, new BasicQuantizer(), new NibbleBlockConverter()))
                .Serialize(new SerializeToFile(_compressedFile + guid));

            // Decompress
            model = new ByteOrder2(alphabet.Length);
            await new FileSource(_compressedFile + guid)
                .ToDecoder(new RANSDecoder<byte, byte>(alphabet, model, new BasicQuantizer(), new NibbleConverter()))
                .Serialize(new SerializeToFile(_decompressedFile + guid));

            TestIntegrity(_decompressedFile + guid, _file);
        }

        private void TestIntegrity(string file1, string file2) =>
            Assert.IsTrue(
                File.ReadAllBytes(file1)
                    .SequenceEqual(File.ReadAllBytes(file2))
            );
    }
}