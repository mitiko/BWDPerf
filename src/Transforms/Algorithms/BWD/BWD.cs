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
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var dictionary = new BWDictionary();

            this.SA = new SuffixArray(buffer); // O(b log m) construction
            Console.WriteLine($"Suffix array took: {timer.Elapsed}"); timer.Restart();
            this.BitVector = new BitVector(buffer.Length, bit: true); // initialize with all bits set
            this.WordCounter.CountAllWords(buffer, this.SA, this.BitVector, this.Options.MaxWordSize);
            Console.WriteLine($"Initial count took: {timer.Elapsed}"); timer.Restart();

            var rankingTime = new TimeSpan();
            var countingTime = new TimeSpan();
            // if there's no more words to encode, we're done
            for (int i = 0; !this.BitVector.IsEmpty(); i++)
            {
                foreach (var wordCount in this.WordCounter.Counts)
                    this.Ranking.Rank(wordCount.Key, wordCount.Value);
                rankingTime += timer.Elapsed; timer.Restart();

                var word = this.Ranking.GetTopRankedWords()[0].Word;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                this.WordCounter.RecountSelectedWord(word, buffer, this.SA, this.BitVector, this.Options.MaxWordSize);
                countingTime += timer.Elapsed; timer.Restart();

                Console.WriteLine($"Did word #{i}");
            }
            Console.WriteLine($"Dict size is {dictionary.Count}");
            Console.WriteLine($"Total ranking time: {rankingTime}; Avg: {rankingTime / dictionary.Count}");
            Console.WriteLine($"Total counting time: {countingTime}; Avg: {countingTime / dictionary.Count}");

            return dictionary;
        }
    }
}