using System;
using System.Collections.Generic;
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
            var dictionary = new BWDictionary(this.Options.IndexSize);

            this.SA = new SuffixArray(buffer, this.Options.MaxWordSize); // O(b log m) construction
            // Initialize with all bits set
            this.BitVector = new BitVector(buffer.Length, bit: true);
            this.WordCounter.CountAllWords(buffer, this.SA, this.BitVector, this.Options.MaxWordSize);

            for (int i = 0; i < dictionary.Length; i++)
            {
                // The last word in the dictionary is always an <s> token
                // If the words in the dictionary cover the whole buffer, there might not be an <s> token
                // In future versions, we'll assume entropy coding is used after and the dictionary size won't be limited, rendering the need for stoken useless.
                if (i == dictionary.STokenIndex)
                {
                    dictionary[i] = CollectSTokenData(buffer);
                    break;
                }

                foreach (var wordCount in this.WordCounter.Counts)
                    this.Ranking.Rank(wordCount.Key, wordCount.Value);

                var word = this.Ranking.GetTopRankedWords()[0].Word;
                // Console.WriteLine($"Choosing word: ({word.Location}, {word.Length})");
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                this.WordCounter.RecountSelectedWord(word, buffer, this.SA, this.BitVector, this.Options.MaxWordSize);

                // if there's no more words to encode, we're done
                if (this.BitVector.IsEmpty())
                    break;
            }

            return dictionary;
        }

        internal byte[] CollectSTokenData(ReadOnlyMemory<byte> buffer)
        {
            var stoken = new List<byte>();
            for (int i = 0; i < this.BitVector.Length; i++)
            {
                bool readStoken = false;
                while (this.BitVector[i])
                {
                    if (!readStoken) readStoken = true;
                    // TODO: Add tests with 0xff
                    if (buffer.Span[i] == 0xff)
                        stoken.Add(0xff);
                    stoken.Add(buffer.Span[i]);
                    i++;
                    if (i >= buffer.Length) break;
                }

                if (readStoken)
                    stoken.Add(0xff);
            }
            return stoken.ToArray();
        }
    }
}