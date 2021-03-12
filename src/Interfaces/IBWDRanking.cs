using System;
using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Interfaces
{
    public interface IBWDRanking
    {
        void Initialize(ReadOnlyMemory<byte> buffer);
        public void Rank(Word word, int count, ReadOnlyMemory<byte> buffer);
        public List<RankedWord> GetTopRankedWords();
    }
}