using System;
using System.Collections.Generic;
using System.Linq;

namespace BWDPerf.Tools
{
    public class SuffixArray
    {
        public int[] SA { get; }
        public Dictionary<byte, int> Alphabet { get; } = new();
        public int[] Rank { get; }

        public SuffixArray(ReadOnlyMemory<byte> input)
        {
            // Prefix doubling - taking O(n log n) time
            int n = input.Length;
            var s = input.Span;
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
            var dict = new Dictionary<byte, int>();
            foreach (var sym in s)
            {
                if (!dict.ContainsKey(sym))
                    dict.Add(sym, 1);
                else
                    dict[sym]++;
            }
            var symbolSet = dict.Keys.OrderBy(x => x);
            this.Rank = new int[symbolSet.Count()];
            for (int i = 0; i < symbolSet.Count(); i++)
            {
                var sym = symbolSet.ElementAt(i);
                this.Rank[i] = dict[sym];
                this.Alphabet.Add(sym, i);
            }

            int cum = 0;
            for (int i = 0; i < this.Rank.Length; i++)
            {
                int count = this.Rank[i];
                this.Rank[i] = cum;
                cum += count;
            }
        }

        public void Print()
        {
            Console.WriteLine("Suffix array:");
            foreach (var x in this.SA)
                Console.WriteLine(x);

            Console.WriteLine("CNT:");
            foreach (var x in this.Rank)
                Console.WriteLine(x);
        }

        public int[] Search(ReadOnlyMemory<byte> word)
        {
            var key = this.Alphabet[word.Span[0]];
            int start = this.Rank[this.Alphabet[word.Span[word.Length-1]]];
            // int end =
            // for (int i = word.Length - 1; i >= 0 ; i--)
            // {
            //     start = Rank[key];
            // }

            throw new NotImplementedException();
        }
    }
}