using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    public class LCPMatchFinder : IBWDMatching
    {
        public int MaxWordSize { get; }
        public BWDIndex BWDIndex { get; private set; }

        public LCPMatchFinder(int maxWordSize = 32) => this.MaxWordSize = maxWordSize;

        public void Initialize(BWDIndex BWDIndex) => this.BWDIndex = BWDIndex;

        public IEnumerable<Match> GetMatches()
        {
            int n = this.BWDIndex.SA.Length;
            var matches = new Match[this.MaxWordSize];
            for (int i = 0; i < matches.Length; i++)
                matches[i] = new Match(0, 0, i+1);

            for (int i = 0; i < n; i++)
            {
                // New matching word, increase the count.
                for (int k = 0; k < this.MaxWordSize; k++)
                    matches[k].Count++;

                // Everything up to LCP[i] matches the next entry. The rest we output and reset.
                for (int k = this.BWDIndex.LCP[i]; k < this.MaxWordSize; k++)
                {
                    if (matches[k].Count > 1) yield return matches[k];
                    matches[k].Index = i + 1;
                    matches[k].Count = 0;
                }
            }
        }

        // TODO: Don't parse when the word can't overlap with itself - i.e. for most text
        // Also maybe write better parsing, since genetic data will overlap with itself a lot!
    }
}