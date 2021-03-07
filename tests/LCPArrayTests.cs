using System;
using System.Linq;
using BWDPerf.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BWDPerf.Tests
{
    // [TestClass]
    public class LCPArrayTests
    {
        // [TestMethod]
        public void TestString1()
        {
            var bytes = ToBytes("mississippi");
            var SA = new SuffixArray(bytes);
            var LCP = new LCPArray(bytes, SA);
            throw new NotImplementedException();
            // TODO: Write tests for LCP array
        }

        private byte[] ToBytes(string s) =>
            s.ToCharArray().Select(c => (byte) c).ToArray();
    }
}