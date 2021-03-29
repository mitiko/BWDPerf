using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Interfaces
{
    public interface IBWDMatching
    {
        public BWDIndex BWDIndex { get; }

        public void Initialize(BWDIndex BWDIndex);
        public IEnumerable<Match> GetMatches();
        public void UpdateState(Word chosenWord);
    }
}