using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Interfaces
{
    public interface IBWDRankProvider
    {
        public BWDIndex BWDIndex { get; }

        public void Initialize(BWDIndex BWDIndex, IBWDMatchProvider matchProvider);
        public void Rank(Match match);
        public List<RankedWord> GetTopRankedWords();
    }
}