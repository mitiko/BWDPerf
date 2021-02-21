using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Tools;
using BWDPerf.Transforms.Algorithms.BWD.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    internal class BWD
    {
        internal Options Options { get; }
        internal SuffixArray SA { get; set; }

        internal BWD(Options options) => this.Options = options;

        internal BWDictionary CalculateDictionary(ReadOnlyMemory<byte> buffer)
        {
            var dictionary = new BWDictionary(this.Options.IndexSize);
            int[][] wordRef = new int[this.Options.MaxWordSize][];
            var wordCount = new OccurenceDictionary<Word>();

            this.SA = new SuffixArray(buffer); // O(b log b)
            FindAllMatchingWords(buffer, ref wordRef); // Initialize words -> O(mb log b)
            this.SA = null; // Deallocate the suffix array, since we're not using it anymore
            CountWords(ref wordRef, ref wordCount); // Count the matching words

            for (int i = 0; i < dictionary.Length; i++)
            {
                Word word;
                if (i == dictionary.STokenIndex)
                {
                    // The last word in the dictionary is always an <s> token
                    // If the words in the dictionary cover the whole buffer, there might not be an <s> token
                    dictionary[i] = CollectSTokenData(buffer, ref wordRef, dictionary);
                    break;
                }

                word = GetHighestRankedWord(ref wordCount);
                // Save the word to the dictionary
                dictionary[i] = buffer.Slice(word.Location, word.Length).ToArray();
                SplitByWord(buffer, word, ref wordRef, ref wordCount);
                CountWords(ref wordRef, ref wordCount);

                // When all references have been encoded, save the dictionary size and exit
                if (wordCount.Values.Sum() == 0) // TODO: can this be just .count
                    break;
            }

            return dictionary;
        }

        internal double Rank(Word word, ref OccurenceDictionary<Word> wordCount)
        {
            var c = wordCount[word]; // Count of this word
            var l = word.Length;
            return (l * this.Options.BPC - this.Options.IndexSize) * (c - 1);
        }

        internal void FindAllMatchingWords(ReadOnlyMemory<byte> buffer, ref int[][] wordRef)
        {
            // Initialize the matrix
            for (int i = 0; i < wordRef.Length; i++)
            {
                wordRef[i] = new int[buffer.Length - i];
                for (int j = 0; j < wordRef[i].Length; j++)
                    wordRef[i][j] = -2;
            }

            for (int i = 0; i < wordRef.Length; i++)
            {
                for (int j = 0; j < wordRef[i].Length; j++)
                {
                    if (wordRef[i][j] != -2) continue;
                    wordRef[i][j] = j;

                    var searchResults = this.SA.Search(data: buffer, word: buffer.Slice(j, i + 1));
                    int lastMatch = j;
                    for (int s = 0; s < searchResults.Length; s++)
                    {
                        int pos = searchResults[s];
                        if (pos >= lastMatch + i + 1)
                        {
                            wordRef[i][pos] = j;
                            lastMatch = pos;
                        }
                    }
                }
            }
        }

        internal void CountWords(ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
        {
            wordCount.Clear();
            for (int i = 0; i < wordRef.Length; i++)
            {
                for (int j = 0; j < wordRef[i].Length; j++)
                {
                    if (wordRef[i][j] == -2)
                        throw new Exception("The matching hasn't covered all the words");

                    if (wordRef[i][j] != -1)
                        wordCount.Add(new Word(wordRef[i][j], i + 1));
                }
            }
        }

        internal Word GetHighestRankedWord(ref OccurenceDictionary<Word> wordCount)
        {
            var bestWord = wordCount.Keys.First();
            double rank, newRank;
            rank = Rank(bestWord, ref wordCount);
            foreach (var word in wordCount.Keys)
            {
                newRank = Rank(word, ref wordCount);
                if (newRank >= rank) { bestWord = word; rank = newRank; }
                // TODO: Make considerations on the contexts from which the words were taken
            }

            return bestWord;
        }

        internal void SplitByWord(ReadOnlyMemory<byte> buffer, Word word, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
        {
            var locations = new int[wordCount[word]]; int x = 0;
            for (int j = 0; j < buffer.Length; j++)
            {
                // Find all locations of this word l
                if (j >= wordRef[word.Length - 1].Length) break;
                if (wordRef[word.Length - 1][j] != word.Location) continue;
                locations[x++] = j;
                j += word.Length - 1;
            }

            for (int l = 0; l < locations.Length; l++)
            {
                for (int i = 0; i < wordRef.Length; i++)
                {
                    // Define start and end of exclusion region
                    int start = locations[l] - i;
                    int end = locations[l] + word.Length - 1;
                    // Enforce bounds
                    start = start >= 0 ? start : 0;
                    end = end < wordRef[i].Length ? end : wordRef[i].Length - 1;
                    // Mark as unavailable
                    for (int s = start; s <= end; s++)
                        wordRef[i][s] = -1;
                }
            }
        }

        internal byte[] CollectSTokenData(ReadOnlyMemory<byte> buffer, ref int[][] wordRef, BWDictionary dictionary)
        {
            var list = new List<byte>();
            // This is the same parsing code as in encode dictionary.
            // TODO: Extract parsing code in a new method
            // Or we can also use the wordRef / bitvector to see which places remain uncovered and collect the stoken like that
            var data = new int[buffer.Length];
            var stoken = dictionary.STokenIndex;
            for (int k = 0; k < data.Length; k++)
                data[k] = stoken;

            for (int i = 0; i < dictionary.WordCount; i++)
            {
                var word = dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != stoken) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer.Span[j + s] != word.Span[s] || data[j+s] != stoken) { match = false; break; }

                    if (match == true)
                    {
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = i;
                    }
                }
            }

            for (int k = 0; k < data.Length; k++)
            {
                bool readStoken = false;
                while (data[k] == stoken)
                {
                    if (!readStoken) readStoken = true;
                    list.Add(buffer.Span[k]);
                    k++;
                    if (k>= data.Length) break;
                }

                if (readStoken)
                    list.Add(0xff);
            }
            return list.ToArray();
        }
    }
}