using BWDPerf.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class BitVectorTests
    {
        [TestMethod]
        public void TestAcess()
        {
            var vector = new BitVector(34);
            vector[2] = true;
            vector[3] = true;
            vector[4] = true;
            vector[17] = true;
            vector[33] = true;
            vector[7] = true;
            vector[3] = false;
            Assert.IsTrue(vector[2]);
            Assert.IsFalse(vector[3]);
            Assert.IsTrue(vector[4]);
            Assert.IsTrue(vector[17]);
            Assert.IsTrue(vector[33]);
            Assert.IsTrue(vector[7]);
        }

        [TestMethod]
        public void TestOutOfRange()
        {
            var vector = new BitVector(12);
            Assert.ThrowsException<System.IndexOutOfRangeException>(() => {
                vector[13] = true;
            });
        }
    }
}