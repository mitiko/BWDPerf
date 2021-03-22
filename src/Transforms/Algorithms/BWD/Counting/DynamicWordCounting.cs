using System;
using System.Collections.Generic;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD.Counting
{
    public class DynamicWordCounting
    {
        public Dictionary<Word, int> Counts { get; set; } = new();

        public void CountAllRepeatedWords(ReadOnlyMemory<byte> buffer, SuffixArray SA, BitVector bitVector, int maxWordSize)
        {
            var LCP = new LCPArray(buffer, SA);
            var matches = new List<int>[maxWordSize];
            for (int i = 0; i < matches.Length; i++)
                matches[i] = new List<int>();

            for (int i = 0; i < SA.Length; i++)
            {
                var curr = SA[i];

                // Matched
                for (int k = 0; k < maxWordSize && curr + k < buffer.Length; k++)
                    matches[k].Add(curr);

                int matchLength = i == LCP.Length ? 0 : LCP[i];

                // Not matched
                for (int k = matchLength; k < maxWordSize; k++)
                {
                    if (matches[k].Count == 0) break;
                    var (word, count) = CountWord(matches[k], k+1, bitVector);
                    if (count > 1)
                        this.Counts.Add(word, count);
                    matches[k].Clear();
                }
            }
        }

        public void RecountSelectedWord(Word word, ReadOnlyMemory<byte> buffer, SuffixArray SA, BitVector bitVector, int maxWordSize)
        {
            // Parse - get locations
            // Get adjacent words
            // Set bitvector to 0s for our word == splitting
            // Count every word in the adjacent list and upate the dictionarys
            var matches = SA.Search(buffer, buffer.Slice(word.Location, word.Length));
            var locations = Parse(matches, word.Length, bitVector);
            if (locations.Count == 0)
            {
                Console.WriteLine($"Matches: {matches.Length}");
                Console.WriteLine($"word: ({word.Location}, {word.Length})");
                Console.WriteLine($"Supposed count: {this.Counts[word]}");
                throw new Exception("Trying to recount a word that doesn't exist");
            }
            var adjacentWords = new List<Word>();
            var containedWords = new List<Word>();
            int loc, endLoc;
            for (int l = 0; l < locations.Count; l++)
            {
                loc = locations[l];
                endLoc = locations[l] + word.Length - 1;

                // Words before
                for (int start = loc - maxWordSize + 1; start < loc; start++)
                {
                    if (start < 0) continue;
                    for (int end = start + maxWordSize - 1; end >= loc; end--)
                    {
                        if (end >= buffer.Length) continue;
                        adjacentWords.Add(new Word(start, end - start + 1));
                    }
                }

                // Words after
                for (int start = endLoc; start >= loc; start--)
                {
                    for (int end = start + maxWordSize - 1; end > endLoc; end--)
                    {
                        if (end >= buffer.Length) continue;
                        adjacentWords.Add(new Word(start, end - start + 1));
                    }
                }

            }
            loc = locations[0];
            endLoc = loc + word.Length - 1;
            // Contained words
            for (int start = loc; start <= endLoc; start++)
            {
                for (int end = endLoc; end >= start; end--)
                {
                    adjacentWords.Add(new Word(start, end - start + 1));
                }
            }

            foreach (var adjWord in adjacentWords)
            {
                var adjMatches = SA.Search(buffer, buffer.Slice(adjWord.Location, adjWord.Length));
                var (firstAdjWord, count) = CountWord(adjMatches, adjWord.Length, bitVector);
                if (count == 0)
                    this.Counts.Remove(firstAdjWord);
                else
                    this.Counts[firstAdjWord] = count;
            }
        }

        // TODO: Don't parse when the word can't overlap with itself - i.e. for most text
        // Also maybe write better parsing, since genetic data will overlap with itself a lot!

        private (Word, int) CountWord(List<int> matches, int length, BitVector bitVector)
        {
            matches.Sort();
            return CountWord(matches.ToArray(), length, bitVector);
        }

        private (Word, int) CountWord(int[] sortedMatches, int length, BitVector bitVector)
        {
            var count = 0;
            var lastMatch = - length;
            for (int i = 0; i < sortedMatches.Length; i++)
            {
                var loc = sortedMatches[i];
                if (loc < lastMatch + length) continue;
                bool available = true;
                for (int s = 0; s < length; s++)
                    if (!bitVector[loc+s]) { available = false; break; }
                if (available)
                {
                    lastMatch = loc;
                    count += 1;
                }
            }
            return (new Word(sortedMatches[0], length), count);
        }

        private List<int> Parse(int[] locations, int length, BitVector bitVector)
        {
            var results = new List<int>();

            for (int l = 0; l < locations.Length; l++)
            {
                var start = locations[l]; // start inclusive
                var end = locations[l] + length; // end exclusive

                // Check location hasn't been used
                bool available = true;
                for (int s = start; s < end; s++)
                    if (!bitVector[s]) { available = false; break; }
                if (!available) continue;

                results.Add(start);
                for (int s = start; s < end; s++)
                    bitVector[s] = false;
            }

            return results;
        }
    }
}