using System;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Tools;
using BWDPerf.Transforms.Entities;

namespace BWDPerf.Transforms.Algorithms.BWD
{
    public class BWD
    {
        internal Options Options { get; }
        internal byte[][] Dictionary { get; }
        internal byte[] STokenData { get; set; }
        internal Word SToken { get; set; }

        internal BWD(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.STokenData = new byte[0];
            this.SToken = new Word(-1, 0);
        }

        internal int CalculateDictionary(in byte[] buffer)
        {
            int dictionarySize = 0;
            int[][] wordRef = new int[this.Options.MaxWordSize][];
            var wordCount = new OccurenceDictionary<Word>();

            FindAllMatchingWords(in buffer, ref wordRef); // Initialize words -> O(mb^2)
            CountWords(in buffer, ref wordRef, ref wordCount); // Count the matching words

            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                Word word;
                if (i == this.Dictionary.Length - 1)
                {
                    // The last word in the dictionary is always an <s> token
                    // If the words in the dictionary cover the whole buffer, there might not be an <s> token
                    CollectSTokenData(in buffer);
                    this.Dictionary[i] = this.STokenData;
                    dictionarySize = i + 1;
                    break;
                }

                word = GetHighestRankedWord(ref wordCount);
                // Save the word to the dictionary
                this.Dictionary[i] = buffer[word.Location..(word.Location + word.Length)];
                SplitByWord(in buffer, word, ref wordRef, ref wordCount);
                CountWords(in buffer, ref wordRef, ref wordCount);

                // When all references have been encoded, save the dictionary size and exit
                if (wordCount.Values.Sum() == 0)
                { dictionarySize = i + 1; break; }
            }

            return dictionarySize;
        }

        internal double Rank(Word word, ref OccurenceDictionary<Word> wordCount)
        {
            var c = wordCount[word]; // Count of this word
            var l = word.Length;
            return (l * this.Options.BPC - this.Options.IndexSize) * (c - 1);
        }

        internal void FindAllMatchingWords(in byte[] buffer, ref int[][] wordRef)
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
                Console.WriteLine($"i = {i}");
                for (int j = 0; j < wordRef[i].Length; j++)
                {
                    if (wordRef[i][j] != -2) continue;
                    wordRef[i][j] = j;

                    if (i == 0)
                    {
                        for (int index = j + 1; index < wordRef[i].Length; index++)
                            if (buffer[j] == buffer[index]) wordRef[i][index] = j;
                        continue;
                    }

                    byte[] selection = new byte[i + 1];
                    int l = wordRef[0][j];
                    for (int index = j + 1; index < wordRef[i].Length;)
                    {
                        if (wordRef[0][index] != l) { index++; continue; } // check if first character matches or don't waste my time and space

                        selection = buffer[index..(index + i + 1)];
                        bool match = true;
                        for (int s = 0; s < selection.Length; s++)
                            if (buffer[j + s] != selection[s]) { match = false; break; }

                        if (match == true) { wordRef[i][index] = j; index += (i + 1); }
                        else { index++; }
                    }
                }
            }
        }

        internal void CountWords(in byte[] buffer, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
        {
            wordCount.Clear();
            for (int i = 0; i < wordRef.Length; i++)
            {
                for (int j = 0; j < wordRef[i].Length; j++)
                {
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

        internal void SplitByWord(in byte[] buffer, Word word, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
        {
            var locations = new int[wordCount[word]]; int x = 0;
            for (int j = 0; j < buffer.Length; j++)
            {
                // Find all locations of this word l
                if (j >= wordRef[word.Length - 1].Length) break;
                if (wordRef[word.Length - 1][j] != word.Location) continue;
                locations[x++] = j;
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

        internal void CollectSTokenData(in byte[] buffer)
        {
            var list = new List<byte>();
            // This is the same parsing code as in encode dictionary.
            // TODO: Extract parsing code in a new method
            var data = new int[buffer.Length];
            var stoken = new DictionaryIndex(this.Dictionary.Length - 1);
            for (int k = 0; k < data.Length; k++)
                data[k] = this.Dictionary.Length - 1;

            for (int i = 0; i < this.Dictionary.Length - 1; i++)
            {
                var word = this.Dictionary[i];
                for (int j = 0; j < buffer.Length; j++)
                {
                    // check if location is used
                    if (data[j] != stoken.Index) continue;
                    if (j + word.Length - 1 >= buffer.Length) break; // can't fit word
                    var match = true;
                    for (int s = 0; s < word.Length; s++)
                        if (buffer[j + s] != word[s] || data[j+s] != stoken.Index) { match = false; break; }

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
                while (data[k] == stoken.Index)
                {
                    if (!readStoken) readStoken = true;
                    list.Add(buffer[k]);
                    k++;
                    if (k>= data.Length) break;
                }

                if (readStoken)
                    list.Add(0xff);
            }
            this.STokenData = list.ToArray();
            return;
        }
    }
}