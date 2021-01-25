using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.EntropyCoders;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class rANSStandaloneTests
    {
        private const string _file = "../../../../data/file.md";
        private const string _compressedFile = "../../../file.rANS";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public async Task TestCompression()
        {
            var compressTask = new BufferedFileSource(_file, 1_000) // 1KB
                .ToCoder(new rANS())
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;
        }

        [TestMethod]
        public async Task TestDecompression()
        {
            var compressTask = new FileSource(_file)
                .ToDecoder(new rANS())
                .Serialize(new SerializeToFile(_compressedFile));

            await compressTask;
        }
    }
}