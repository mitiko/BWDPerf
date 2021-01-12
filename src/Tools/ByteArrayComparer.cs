using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BWDPerf.Tools
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            if ( left == null || right == null ) return left == right;
            return left.SequenceEqual(right);
        }

        public int GetHashCode(byte[] key)
        {
            return new BigInteger(key).GetHashCode();
        }
    }
}