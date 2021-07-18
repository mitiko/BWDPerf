using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Interfaces
{
    public interface IBWDMatchProvider
    {
        public BWDIndex BWDIndex { get; }

        public void Initialize(BWDIndex BWDIndex);
        public IEnumerable<Match> GetMatches();
        public void RemoveIfPossible(Match match);
    }
}