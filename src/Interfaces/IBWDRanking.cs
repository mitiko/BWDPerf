using System;
using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Interfaces
{
    public interface IBWDRanking
    {
        public void Rank(Word word, int count);
        public List<RankedWord> GetTopRankedWords();
    }
}