using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BWDPerf.Common.Entities;
using BWDPerf.Interfaces;
using BWDPerf.Tools;

namespace BWDPerf.Common.Algorithms.BWD
{
    // Encode the buffer and pass it on as individual symbols or as blocks of indices
    public class BWD : ICoder<byte[], DictionaryIndex[]>
    {
        public Options Options { get; set; }
        public byte[][] Dictionary { get; }
        public CountDictionary<Word> Count { get; set; }
        public int[][] WordRef { get; set; }
        public byte[] SToken { get; set; }
        public int STokenFrequency { get; set; }

        public BWD(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.WordRef = new int[options.IndexSize][];
            // TODO: Add initial count to the creation of a count dictionary so we can just pretend the count is 1 less than actual and save calculations in the ranking
            this.Count = new CountDictionary<Word>();
            this.SToken = new byte[0];
            this.STokenFrequency = 0;
        }

        public async IAsyncEnumerable<DictionaryIndex[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            int k = 0;
            int expectedTotalSavedBits = 0;
            await foreach (var buffer in input)
            {
                var (dictionarySize, expectedSavedBits) = CalculateDictionary(in buffer);
                expectedTotalSavedBits += expectedSavedBits;
                Console.WriteLine($"Expected save of {expectedSavedBits} bits on {k} iteration");
                // Write dcitionary

                var fileWriter = new BinaryWriter(new FileInfo($"dictionary-{k}-{this.GetHashCode()}.bwd.dict").OpenWrite());
                fileWriter.Write(dictionarySize);
                for (int i = 0; i < dictionarySize; i++)
                {
                    var word = this.Dictionary[i];
                    if (usesSToken && i == dictionarySize-1)
                    {
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        fileWriter.Write(this.SToken.Length);
                        foreach (var character in this.SToken) fileWriter.Write(character);
                    }
                    else
                    {
                        fileWriter.Write((byte) word.Length);
                    }
                    fileWriter.Write(word);
                }
                fileWriter.Flush();
                fileWriter.Dispose();
                // Split by dictionary
                // write indices
                // yield return default;
                var dict = new DictionaryIndex[dictionarySize];
                for (int i = 0; i < dictionarySize; i++)
                {
                    var word = this.Dictionary[i];
                    dict[i] = new DictionaryIndex(i, usesSToken ? this.STokenFrequency : this.Words[word]);
                }
                yield return dict;
                k++;
            }
            Console.WriteLine($"Expected total saved bits: {expectedTotalSavedBits}");
        }

        private (int dictionarySize, int expectedSavedBits) CalculateDictionary(in byte[] buffer)
        {
            int dictionarySize = 0; int expectedSavedBits = 0;
            var timer = Stopwatch.StartNew();
            FindAllMatchingWords(in buffer); // Initialize words -> O(mb)
            Console.WriteLine($"Finding all matching words took: {timer.Elapsed}");
            timer.Restart();

            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                bool isLastWord = i == this.Dictionary.Length - 1;
                // Count occurences
                CountWords(in buffer);
                Console.WriteLine($"Counting the words took: {timer.Elapsed}");
                timer.Restart();
                // Get the best word
                var word = GetHighestRankedWord(isLastWord);
                Console.WriteLine($"Getting highest ranked word took: {timer.Elapsed}");
                timer.Restart();
                // Split by word and save it to dictionary
                SplitByWord(in buffer, word, isLastWord);
                Console.WriteLine($"Splitting took: {timer.Elapsed}");
                timer.Restart();

                // Save the chosen word
                this.Dictionary[i] = buffer[word.Location..(word.Location + word.Length)];
                // Calculate estimated savings
                expectedSavedBits += Loss(word, isLastWord);
                Console.WriteLine($"{i} --- {Loss(word, isLastWord)} --- \"{Print(this.Dictionary[i], isLastWord)}\"");

                // When all references have been encoded, save the dictionary size and exit
                if (this.Count.Values.Sum() == 0)
                { dictionarySize = i + 1; break; }
            }

            return (dictionarySize, expectedSavedBits);
        }

        private string Print(byte[] word, bool isSToken)
        {
            // TODO: Change certain bytes for readability (\0x20 and \n, \t etc)
            if (isSToken) return "<s>";
            string str = "";
            foreach (var s in word)
            {
                str += (char) s;
            }
            return str;
        }

        private void FindAllMatchingWords(in byte[] buffer)
        {
            // Initialize the matrix
            for (int i = 0; i < this.WordRef.Length; i++)
                this.WordRef[i] = new int[buffer.Length - i];

            for (int j = 0; j < buffer.Length; j++)
            {
                int startSearch = j - 1; // just a hack for faster search, otherwise set to a constant j-1
                for (int i = 0; i < this.WordRef.Length; i++)
                {
                    int len = i + 1;
                    if (j + len > buffer.Length) break; // if there's no space for the current word, we just skip
                    byte[] selection = new byte[len]; // initialize the selection window before the loop
                    int matchIndex = -1;

                    for (int index = startSearch; index >= 0; index--)
                    {
                        int end = index + len; // start + len;
                        if (end >= j) continue; // if the words overlap, no match can be found
                        selection = buffer[index..end]; // select the search region

                        // Check if selection matches the word at j by comparing them.
                        bool match = true;
                        for (int s = 0; s < len; s++)
                            if (selection[s] != buffer[j+s]) { match = false; break; }

                        if (match) { matchIndex = index; break; }
                    }

                    // If no match has been found, set this as the first occurence
                    this.WordRef[i][j] = matchIndex != -1 ? matchIndex : j;
                    // Remember that the first backwards match of this length from this location is at this index;
                    startSearch = this.WordRef[i][j];
                }
            }
        }

        private int Rank(Word word)
        {
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Count[word] - 1);
        }

        private int Loss(Word word, bool isLastWord = false)
        {
            if (isLastWord && this.STokenFrequency == 0) return - this.Options.IndexSize;
            if (isLastWord) return - (this.Options.IndexSize + this.Options.BPC) * this.STokenFrequency;
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Count[word] - 1) - this.Options.IndexSize;
        }

        private Word GetHighestRankedWord(bool isLastWord)
        {
            var bestWord = this.Words.First().Key;
            int rank, newRank;
            rank = Rank(bestWord);
            foreach (var word in this.Words.Keys)
            {
                newRank = Rank(word);
                if (newRank > rank)
                { bestWord = word; rank = newRank; }
                if (newRank == rank && this.Words[word] >= this.Words[bestWord])
                { bestWord = word; }
            }

            if (Loss(bestWord) <= Loss(this.SToken, true) && this.Options.AutoEnd)
                usesSToken = true;

            if (usesSToken)
                return this.SToken;

            return bestWord;
        }

        private void SplitByWord(in byte[] buffer, Word word, bool isLastWord)
        {
            // TODO: Use s token
            // if (isLastWord)
            // {
            //     this.SToken = this.SToken.Concat(buffer).ToArray();
            //     this.STokenFrequency++;
            //     continue;
            // }

            for (int l = 0; l < buffer.Length; l++)
            {
                // Find all locations of this word l
                if (this.WordRef[word.Length - 1][l] != word.Location) continue;

                for (int j = 0; j < buffer.Length; j++)
                {
                    for (int i = 0; i < this.WordRef.Length; i++)
                    {
                        // Define start and end of exclusion region
                        int start = l - i;
                        int end = l + word.Length - 1 + i;
                        // Enforce bounds
                        start = start >= 0 ? start : 0;
                        end = end < buffer.Length ? end : buffer.Length - 1;
                        // Mark as unavailable
                        for (int s = start; s <= end; s++)
                            this.WordRef[i][s] = -1;
                    }
                }
            }
        }

        private void CountWords(in byte[] buffer)
        {
            this.Count.Clear();
            for (int j = 0; j < buffer.Length; j++)
            {
                for (int i = 0; i < this.WordRef.Length; i++)
                {
                    if (this.WordRef[i][j] != j && this.WordRef[i][j] != -1)
                        this.Count.Add(new Word(this.WordRef[i][j], i + 1));
                }
            }
        }
    }
}