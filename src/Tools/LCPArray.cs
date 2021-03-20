using System;

namespace BWDPerf.Tools
{
    public class LCPArray
    {
        private int[] LCP { get; }

        public int this[int index] => this.LCP[index];
        public int Length => this.LCP.Length;

        // kasai algorithm for LCP construction from SA in O(n)
        public LCPArray(ReadOnlyMemory<byte> buffer, SuffixArray SA)
        {
            var T = buffer.Span;
            int n = T.Length;
            LCP = new int[n-1];

            var SAinv = new int[n];
            for (int i = 0; i < n; i++)
                SAinv[SA[i]] = i;

            int k = 0;
            for (int i = 0; i < n; i++)
            {
                if (SAinv[i] == n-1)
                {
                    k = 0;
                    continue;
                }
                int j = SA[SAinv[i]+1];

                while (true)
                {
                    if (i+k >= n) break;
                    if (j+k >= n) break;
                    if (T[i+k] != T[j+k]) break;
                    k++;
                }

                LCP[SAinv[i]] = k;
                if (k > 0) k--;
            }
        }
    }
}