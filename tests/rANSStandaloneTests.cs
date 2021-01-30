using System.Collections.Generic;
using System.Threading.Tasks;
using BWDPerf.Architecture;
using BWDPerf.Transforms.Algorithms.EntropyCoders;
using BWDPerf.Transforms.Serializers;
using BWDPerf.Transforms.Sources;
using BWDPerf.Transforms.Models.RANS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BWDPerf.Interfaces;

namespace BWDPerf.Tests
{
    [TestClass]
    public class rANSStandaloneTests
    {
        private const string _file = "../../../../data/file.md";
        private const string _compressedFile = "../../../file.rANS";
        private const string _decompressedFile = "../../../decompressed";

        [TestMethod]
        public void TestCompression()
        {
            var collectData = new CollectData();
            var initial = new Dictionary<byte, int>();
            var stream = new BufferedFileSource(_file, 1_000) // 1KB
                .ToCoder(new rANS<byte>(new Order0<byte>(initial)));
                // .Serialize(new SerializeToFile(_compressedFile));

            // await compressTask;
        }

        [TestMethod]
        public async Task TestDecompression()
        {
            var compressTask = new FileSource(_compressedFile)
                .ToDecoder(new rANS<byte>())
                .Serialize(new SerializeToFile(_decompressedFile));

            await compressTask;
        }
    }

    internal class CollectData : ICoder<byte[], byte[]>
    {

    }
}