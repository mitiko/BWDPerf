using System.Collections.Generic;
using System.Linq;

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
            uint hash = 0;
            for (int i = key.Length - 1; i >= 0; i--)
                hash = (key[i] + hash + 1) * 2654435761;
            return (int)hash;
        }
    }
}