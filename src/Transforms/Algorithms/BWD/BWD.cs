using System;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Counting;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    internal class BWD
    {
        internal Options Options { get; }
        internal IBWDRanking Ranking { get; }
        internal DynamicWordCounting WordCounter { get; } = new();
        internal SuffixArray SA { get; set; }
        internal BitVector BitVector { get; private set; }

        internal BWD(Options options, IBWDRanking ranking)
        {
            this.Options = options;
            this.Ranking = ranking;
        }

        internal BWDictionary CalculateDictionary(ReadOnlyMemory<byte> buffer)
        {
            var dictionary = new BWDictionary();

            this.SA = new SuffixArray(buffer); // O(b log m) construction
            this.BitVector = new BitVector(buffer.Length, bit: true); // initialize with all bits set
            this.WordCounter.CountAllWords(buffer, this.SA, this.BitVector, this.Options.MaxWordSize);

            // if there's no more words to encode, we're done
            for (int i = 0; !this.BitVector.IsEmpty(); i++)
            {
                foreach (var wordCount in this.WordCounter.Counts)
                    this.Ranking.Rank(wordCount.Key, wordCount.Value);

                var word = this.Ranking.GetTopRankedWords()[0].Word;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                this.WordCounter.RecountSelectedWord(word, buffer, this.SA, this.BitVector, this.Options.MaxWordSize);
            }

            return dictionary;
        }
    }
}