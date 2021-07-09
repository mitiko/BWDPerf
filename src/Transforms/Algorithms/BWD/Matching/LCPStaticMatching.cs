using System;
using System.Collections.Generic;
using System.Diagnostics;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    public class LCPStaticMatching : IBWDMatching
    {
        public BWDIndex BWDIndex { get; private set; }
        private Match[] Matches { get; set; }

        public void Initialize(BWDIndex BWDIndex)
        {
            this.BWDIndex = BWDIndex;
            int count = 0;
            foreach (var _ in GetMatchesDynamically()) count++;

            this.Matches = new Match[count];
            int i = 0;
            foreach (var m in GetMatchesDynamically())
                this.Matches[i++] = new Match(m.Index, m.Count, m.Length);
        }

        public IEnumerable<Match> GetMatches() => this.Matches;

        private IEnumerable<Match> GetMatchesDynamically()
        {
            int n = this.BWDIndex.Length;
            var matches = new Stack<Match>();

            for (int i = 0; i < n; i++)
            {
                var lcp = this.BWDIndex.LCP[i];
                if (matches.Count == 0)
                {
                    for (int l = 2; l <= lcp; l++)
                        matches.Push(new Match(i, 0, l));
                }

                while (matches.TryPeek(out var match) && lcp < match.Length)
                {
                    var m = matches.Pop();
                    m.Count = i - m.Index + 1;
                    yield return m;
                }
                while (matches.TryPeek(out var match) && lcp > match.Length)
                {
                    matches.Push(new Match(i, 0, match.Length + 1));
                }
            }
            Debug.Assert(matches.Count == 0, "There were matches left in the LCP stack.");
        }
    }
}