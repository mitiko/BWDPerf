using System;
using System.Collections.Generic;
using System.Linq;

namespace BWDPerf.Tools
{
    public class SuffixArray
    {
        public int[] SA { get; }

        public SuffixArray(ReadOnlyMemory<byte> data)
        {
            // Prefix doubling - taking O(n log n) time
            int n = data.Length;
            var s = data.Span;
            const int alphabet = 256;
            var p = new int[n];
            var c = new int[n];
            var cnt = new int[Math.Max(n, alphabet)];

            for (int i = 0; i < n; i++)
                cnt[s[i]]++;
            for (int i = 1; i < alphabet; i++)
                cnt[i] += cnt[i-1];
            for (int i = 0; i < n; i++)
                p[--cnt[s[i]]] = i;
            c[p[0]] = 0;
            int classes = 1;
            for (int i = 1; i < n; i++)
            {
                if (s[p[i]] != s[p[i-1]])
                    classes++;
                c[p[i]] = classes - 1;
            }

            var pn = new int[n];
            var cn = new int[n];
            for (int h = 0; (1 << h) < n; ++h)
            {
                for (int i = 0; i < n; i++)
                {
                    pn[i] = p[i] - (1 << h);
                    if (pn[i] < 0)
                        pn[i] += n;
                }
                for (int i = 0; i < classes; i++)
                    cnt[i] = 0;
                for (int i = 0; i < n; i++)
                    cnt[c[pn[i]]]++;
                for (int i = 1; i < classes; i++)
                    cnt[i] += cnt[i-1];
                for (int i = n-1; i >= 0; i--)
                    p[--cnt[c[pn[i]]]] = pn[i];
                cn[p[0]] = 0;
                classes = 1;
                for (int i = 1; i < n; i++) {
                    var cur = (c[p[i]], c[(p[i] + (1 << h)) % n]);
                    var prev = (c[p[i-1]], c[(p[i-1] + (1 << h)) % n]);
                    if (cur != prev)
                        ++classes;
                    cn[p[i]] = classes - 1;
                }
                c = cn;
            }

            this.SA = p;
        }

        public void Print()
        {
            Console.WriteLine("Suffix array:");
            foreach (var x in this.SA)
                Console.WriteLine(x);
        }

        public int[] Search(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> word)
        {
            // Binary search on the sorted suffix array takes O(m log n) worst case for word size m
            // FM-index stores BWT and can search in O(m) with a constant rank function using a wavelet tree
            int low = 0;
            int high = data.Length;
            int match = -1;
            while (low < high)
            {
                int mid = (low + high) / 2;
                for (int i = 0; i < word.Length; i++)
                {
                    if (this.SA[mid] + i >= data.Length) { low = mid + 1; break; }
                    var sym = data.Span[this.SA[mid] + i];
                    if (sym == word.Span[i])
                    {
                        if (i == word.Length - 1)
                            match = mid;
                        continue;
                    }

                    if (word.Span[i] > sym)
                        low = mid + 1;
                    if (word.Span[i] < sym)
                        high = mid;
                    break;
                }
                if (match != -1)
                    break;
            }

            if (match == -1)
                return new int[0];

            int first = match; // first match inclusive
            int last = match;  // last  match exclusive
            while (IsMatch(first - 1))
                first--;
            while (IsMatch(last))
                last++;

            return this.SA[first..last];

            bool IsMatch(int index)
            {
                if (index < 0) return false;
                if (index >= this.SA.Length) return false;
                var pos = this.SA[index];
                if (pos + word.Length - 1 >= data.Length) return false;
                for (int i = 0; i < word.Length; i++)
                    if (data.Span[pos + i] != word.Span[i]) return false;
                return true;
            }
        }
    }
}