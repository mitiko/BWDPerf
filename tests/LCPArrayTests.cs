using System;
using System.Linq;
using BWDPerf.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class LCPArrayTests
    {
        [TestMethod]
        public void TestString1()
        {
            var bytes = ToBytes("mississippi");
            var SA = new SuffixArray(bytes);
            var LCP = new LCPArray(bytes, SA, out _);
            var ans = new int[] {1,1,4,0,0,1,0,2,1,3,0};
            Assert.AreEqual(LCP.Length, ans.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(LCP[i], ans[i]);
        }

        [TestMethod]
        public void TestString2()
        {
            var bytes = ToBytes("Wolloomooloo");
            var SA = new SuffixArray(bytes);
            var LCP = new LCPArray(bytes, SA, out _);
            var ans = new int[] {0,1,3,0,0,1,2,1,1,2,2,0};
            Assert.AreEqual(ans.Length, LCP.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(ans[i], LCP[i]);
        }

        [TestMethod]
        public void TestString3()
        {
            var bytes = ToBytes("bababdbabdbbabbdbabbbabdb");
            var SA = new SuffixArray(bytes);
            var LCP = new LCPArray(bytes, SA, out _);
            var ans = new int[] {2,3,2,4,4,0,1,3,4,3,5,5,1,4,2,2,1,3,5,3,0,2,4,2,0};
            Assert.AreEqual(LCP.Length, ans.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(LCP[i], ans[i]);
        }

        private static byte[] ToBytes(string s) =>
            s.ToCharArray().Select(c => (byte) c).ToArray();
    }
}