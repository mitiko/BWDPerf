using System.Linq;
using BWDPerf.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    [TestClass]
    public class SuffixArrayTests
    {
        [TestMethod]
        public void TestString1()
        {
            var bytes = ToBytes("mississippi");
            var sa = new SuffixArray(bytes);
            var ans = new int[] {10,7,4,1,0,9,8,6,3,5,2};
            Assert.AreEqual(sa.SA.Length, ans.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(sa.SA[i], ans[i]);
        }

        [TestMethod]
        public void TestString2()
        {
            var bytes = ToBytes("Wolloomooloo");
            var sa = new SuffixArray(bytes);
            var ans = new int[] {0,2,9,3,6,11,1,8,5,10,7,4};
            Assert.AreEqual(sa.SA.Length, ans.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(sa.SA[i], ans[i]);
        }

        [TestMethod]
        public void TestSearch()
        {
            var bytes = ToBytes("bababdbabdbbabbdbabbbabdb");
            var sa = new SuffixArray(bytes);
            var ans = new int[] {1,17,12,21,3,7,24,0,16,11,20,2,6,10,19,18,13,22,14,4,8,23,15,5,9};
            Assert.AreEqual(sa.SA.Length, ans.Length);
            for (int i = 0; i < ans.Length; i++)
                Assert.AreEqual(sa.SA[i], ans[i]);
            var searchResults = sa.Search(bytes, ToBytes("bb"));
            var sr = new int[] {10,13,18,19};
            Assert.AreEqual(searchResults.Length, sr.Length);
            for (int i = 0; i < sr.Length; i++)
                Assert.AreEqual(searchResults[i], sr[i]);
        }

        private byte[] ToBytes(string s) =>
            s.ToCharArray().Select(c => (byte) c).ToArray();
    }
}