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

namespace BWDPerf.Tests
{
    [TestClass]
    public class RANSTests
    {
        private const string _file = "../../../../data/enwik4";
        private const string _compressedFile = "../../../enwik4.rans";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task TestCompression()
        {
            var alphabet = new TextAlphabet();
            var model = new Order0(alphabet.Length);
            var quantizer = new BasicQuantizer(model);
            var compressTask = new BufferedFileSource(_file, 10_000_000) // 10MB
                .ToCoder(new RANSEncoder<byte>(alphabet, quantizer))
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;
        }

        [TestMethod]
        public async Task TestDecompression()
        {
            var alphabet = new TextAlphabet();
            var model = new Order0(alphabet.Length);
            var quantizer = new BasicQuantizer(model);
            var decompressTask = new FileSource(_compressedFile)
                .ToDecoder(new RANSDecoder<byte>(alphabet, quantizer))
                .Serialize(new SerializeToFile(_decompressedFile));

            await decompressTask;
        }

        [TestMethod]
        public void TestIntegrity() =>
            Assert.IsTrue(
                File.ReadAllBytes(_file)
                    .SequenceEqual(File.ReadAllBytes(_decompressedFile))
            );
    }
}