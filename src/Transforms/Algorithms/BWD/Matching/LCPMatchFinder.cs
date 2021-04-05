using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Matching
{
    public class LCPMatchFinder : IBWDMatching
    {
        public int MaxWordSize { get; private set; } = 4;
        public BWDIndex BWDIndex { get; private set; }

        public void Initialize(BWDIndex BWDIndex)
        {
            this.BWDIndex = BWDIndex;
            for (int i = 0; i < this.BWDIndex.Length; i++)
            {
                if (this.BWDIndex.LCP[i] > this.MaxWordSize) this.MaxWordSize = this.BWDIndex.LCP[i];
            }
            System.Console.WriteLine($"Max size repeated word: {this.MaxWordSize}");
        }

        public IEnumerable<Match> GetMatches()
        {
            int n = this.BWDIndex.Length;
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
                    if (matches[k].Count > 1 && matches[k].Index + matches[k].Length <= n)
                        yield return matches[k];
                    matches[k].Index = i + 1;
                    matches[k].Count = 0;
                }
            }
        }
    }
}