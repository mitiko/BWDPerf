using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    internal class BWD
    {
        internal Options Options { get; }
        internal IBWDRanking Ranking { get; }
        internal SuffixArray SA { get; set; }
        internal BitVector BitVector { get; private set; }

        internal BWD(Options options, IBWDRanking ranking)
        {
            this.Options = options;
            this.Ranking = ranking;
        }

        internal BWDictionary CalculateDictionary(ReadOnlyMemory<byte> buffer)
        {
            // Inspect all methods
            // Select locations that need to be recounted
            // Keep a bit vector of counted places
            // When splitting, zero out the locations that need to be recounted
            // Recount these locations until the bitvector is full

            var dictionary = new BWDictionary(this.Options.IndexSize);

            this.SA = new SuffixArray(buffer, this.Options.MaxWordSize); // O(b log m) construction
            this.BitVector = new BitVector(buffer.Length, bit: true);
            RankAllWords(buffer);
            // Initialize with all bits set

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

                RankAllWords(buffer);
                var word = this.Ranking.GetTopRankedWords()[0].Word;
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                SplitByWord(buffer, word);

                // if there's no more words to encode, we're done
                if (this.BitVector.IsEmpty())
                    break;
            }

            return dictionary;
        }

        internal void RankAllWords(ReadOnlyMemory<byte> buffer)
        {
            var matches = new List<int>[this.Options.MaxWordSize];
            for (int i = 0; i < matches.Length; i++)
                matches[i] = new List<int>();

            for (int n = 0; n < this.SA.Length - 1; n++)
            {
                var curr = this.SA[n];
                var next = this.SA[n+1];

                // Matched
                for (int k = 0; k < this.Options.MaxWordSize && curr + k < buffer.Length; k++)
                    matches[k].Add(curr);

                int matchLength = 0;
                for (matchLength = 0; matchLength < this.Options.MaxWordSize; matchLength++)
                {
                    if (curr + matchLength >= buffer.Length) break;
                    if (next + matchLength >= buffer.Length) break;
                    if (buffer.Span[curr + matchLength] != buffer.Span[next + matchLength]) break;
                }

                // Not matched
                for (int k = matchLength; k < this.Options.MaxWordSize; k++)
                {
                    if (matches[k].Count == 0) break;
                    var (word, count) = CountWord(matches[k], k+1);
                    this.Ranking.Rank(word, count);
                    matches[k].Clear();
                }
            }
            // TODO: fix last word
            // matches.Add(this.SA[this.SA.Length - 1]);
            // var m = CountWord(matches, j + 1);
            // // rank
            // wc.Add(m.word, m.count);
        }

        internal void SplitByWord(ReadOnlyMemory<byte> buffer, Word word)
        {
            // TODO: Suffix array search by word;
            // var locations = this.SA.Search(buffer, word);
            var locations = this.SA.Search(buffer, buffer.Slice(word.Location, word.Length));
            for (int l = 0; l < locations.Length; l++)
            {
                var start = locations[l]; // start inclusive
                var end = locations[l] + word.Length; // end exclusive

                // Check location hasn't been used
                bool available = true;
                for (int s = start; s < end; s++)
                    if (!this.BitVector[s]) { available = false; break; }
                if (!available) continue;

                for (int s = start; s < end; s++)
                    this.BitVector[s] = false;
            }
        }

        internal byte[] CollectSTokenData(ReadOnlyMemory<byte> buffer)
        {
            // TODO: add extra seperator when the stoken contains the seprator symbol
            // TODO: implement rank in bitvector
            // var stoken = new List<byte>(capacity: this.BitVector.Rank(bit: true));
            var stoken = new List<byte>();
            for (int i = 0; i < this.BitVector.Length; i++)
            {
                bool readStoken = false;
                while (this.BitVector[i])
                {
                    if (!readStoken) readStoken = true;
                    // TODO: Check if the decoder knows about that. I don't think the tests have 0xff
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

        internal (Word word, int count) CountWord(List<int> matches, int length)
        {
            matches.Sort();
            int curr = matches[0];
            int count = 1;
            foreach (var next in matches)
            {
                if (next >= curr + length)
                {
                    bool available = true;
                    for (int i = 0; i < length; i++)
                        if (!this.BitVector[next+i]) { available = false; break; }
                    if (available)
                    {
                        curr = next;
                        count++;
                    }
                }
            }
            return (new Word(curr, length), count);
        }
    }
}