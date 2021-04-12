using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    public class LCPMatchFinder : IBWDMatching
    {
        public BWDIndex BWDIndex { get; private set; }

        public void Initialize(BWDIndex BWDIndex) => this.BWDIndex = BWDIndex;

        public IEnumerable<Match> GetMatches()
        {
            int n = this.BWDIndex.Length;
            var matches = new Stack<Match>();

            for (int i = 0; i < n; i++)
            {
                var lcp = this.BWDIndex.LCP[i];
                if (matches.Count == 0)
                {
                    if (lcp >= 2) matches.Push(new Match(i, 0, lcp));
                    continue;
                }
                while (matches.TryPeek(out var match))
                {
                    if (lcp < match.Length)
                    {
                        var m = matches.Pop();
                        m.Count = i - m.Index + 1;
                        yield return m;
                    }
                    else if (lcp > match.Length)
                    {
                        matches.Push(new Match(i, 0, lcp));
                        break;
                    }
                    else break;
                }
            }
        }
    }
}