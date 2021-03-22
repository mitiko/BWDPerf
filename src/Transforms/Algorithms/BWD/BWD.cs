using System;
using System.Linq;
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
            this.WordCounter.CountAllRepeatedWords(buffer, this.SA, this.BitVector, this.Options.MaxWordSize);
            Console.WriteLine($"Initial count took: {timer.Elapsed}"); timer.Restart();

            Console.WriteLine($"Words with count 1: {this.WordCounter.Counts.Count(x => x.Value == 1)}");
            Console.WriteLine($"Words with count 2: {this.WordCounter.Counts.Count(x => x.Value == 2)}");
            Console.WriteLine($"Words with count 3: {this.WordCounter.Counts.Count(x => x.Value == 3)}");
            Console.WriteLine($"Words with count 4: {this.WordCounter.Counts.Count(x => x.Value == 4)}");
            Console.WriteLine($"Total words: {this.WordCounter.Counts.Count}");

            this.Ranking.Initialize(buffer);

            var rankingTime = new TimeSpan();
            var countingTime = new TimeSpan();
            // if there's no more words to encode, we're done
            for (int i = 0; !this.BitVector.IsEmpty(); i++)
            {
                foreach (var wordCount in this.WordCounter.Counts)
                    this.Ranking.Rank(wordCount.Key, wordCount.Value, buffer);
                rankingTime += timer.Elapsed; timer.Restart();

                var word = this.Ranking.GetTopRankedWords()[0].Word;
                if (word.Equals(Word.Empty))
                    break;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();

                var str = "";
                foreach (var symbol in dictionary[i].Span)
                    str += (char) symbol;
                Console.WriteLine($"word -- '{str}'; {word.Location} {word.Length}");

                this.WordCounter.RecountSelectedWord(word, buffer, this.SA, this.BitVector, this.Options.MaxWordSize);
                countingTime += timer.Elapsed; timer.Restart();
            }

            // If there are no more good words, add the remaining of the individual symbols to the dictionary
            for (int i = dictionary.Count; !this.BitVector.IsEmpty(); i++)
            {
                var symbol = Word.Empty;
                for (int j = 0; j < buffer.Length; j++)
                {
                    if (this.BitVector[j])
                    {
                        symbol = new Word(j, 1);
                        var locations = this.SA.Search(buffer, buffer.Slice(j, 1));
                        for (int l = 0; l < locations.Length; l++) this.BitVector[locations[l]] = false;
                        break;
                    }
                }
                dictionary[i] = buffer.Slice(symbol.Location, symbol.Length);
            }

            Console.WriteLine($"Dict size is {dictionary.Count}");
            Console.WriteLine($"Total ranking time: {rankingTime}; Avg: {rankingTime / dictionary.Count}");
            Console.WriteLine($"Total counting time: {countingTime}; Avg: {countingTime / dictionary.Count}");

            return dictionary;
        }
    }
}