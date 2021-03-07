using System;

namespace BWDPerf.Tools
{
    public class LCPArray
    {
        private int[] LCP { get; }

        public int this[int index] => this.LCP[index];
        public int Length => this.LCP.Length;

        public LCPArray(ReadOnlyMemory<byte> buffer, SuffixArray SA)
        {
            var T = buffer.Span;
            int n = T.Length;
            var Phi = new int[n];
            var PLCP = new int[n];
            this.LCP = new int[n-1];

            for (int i = 1; i < n; i++)
                Phi[SA[i]] = SA[i-1];

            int l = 0;
            for (int j = 0; j < n; j++)
            {
                while (true)
                {
                    var phi = Phi[j];
                    if (j+l >= n) break;
                    if (phi+l >= n) break;
                    if (T[j+l] != T[phi+l]) break;
                    l++;
                }
                PLCP[j] = l;
                if (l > 0) l--;
            }

            for (int i = 0; i < n-1; i++)
                this.LCP[i] = PLCP[SA[i+1]];
        }
    }
}