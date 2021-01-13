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
        public Dictionary<byte[], int> Words { get; set; }
        public int[][] Arr { get; set; }
        public byte[] SToken { get; set; }
        public int STokenFrequency { get; set; }

        public BWD(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.Arr = new int[options.IndexSize][];
            this.SToken = new byte[0];
            this.STokenFrequency = 0;
        }

        public async IAsyncEnumerable<DictionaryIndex[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            int k = 0;
            int totalSaved = 0;
            await foreach (var buffer in input)
            {
                var (usesSToken, dictionarySize, savedBits) = CalculateDictionary(in buffer);
                totalSaved += savedBits;
                Console.WriteLine($"Saved {savedBits} on {k} iteration");
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
            Console.WriteLine($"Saved: {totalSaved}");
        }

        private (bool usesSToken, int dictionarySize, int savedBits) CalculateDictionary(in byte[] buffer)
        {
            var timer = Stopwatch.StartNew();
            bool usesSToken = false; int dictionarySize = 0; int savedBits = 0;
            // Initialize words -> O(mb)
            FindAllMatchingWords(in buffer);
            Console.WriteLine($"Finding all matching words took: {timer.Elapsed}");
            timer.Restart();
            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                // Count occurences
                // TODO: count occurences
                // Get the best word
                var word = GetHighestRankedWord(ref usesSToken);
                Console.WriteLine($"Getting highest ranked word took: {timer.Elapsed}");
                timer.Restart();
                // Split by word and save it to dictionary
                SplitByWord(in buffer, word.Length, 0, usesSToken);
                Console.WriteLine($"Splitting took: {timer.Elapsed}");
                timer.Restart();
                this.Dictionary[i] = word;
                savedBits += Loss(word, usesSToken);

                // If reached the end of the dictionary and more data is left,
                // re-run the last iteration with the SToken
                Console.WriteLine($"{i} --- {Loss(word, usesSToken)} --- \"{Print(word, usesSToken)}\"");
                if (i == this.Dictionary.Length - 1 && contexts.Count > 0)
                { i--; usesSToken = true; } // TODO: Changes the contexts and then the s token is applied, making decompression impossible

                // If we've got no data left to encode, save dictionary size
                if (contexts.Count == 0)
                { dictionarySize = i + 1; break; }
            }

            return (usesSToken, dictionarySize, savedBits);
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
            for (int i = 0; i < this.Arr.Length; i++)
                this.Arr[i] = new int[buffer.Length - i];

            for (int j = 0; j < buffer.Length; j++)
            {
                int startSearch = j - 1; // just a hack for faster search, otherwise set to a constant j-1
                for (int i = 0; i < this.Arr.Length; i++)
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
                    this.Arr[i][j] = matchIndex != -1 ? matchIndex : j;
                    // Remember that the first backwards match of this length from this location is at this index;
                    startSearch = this.Arr[i][j];
                }
            }
        }

        private int Rank(byte[] word)
        {
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Words[word] - 1);
        }

        private int Loss(byte[] word, bool isSToken = false)
        {
            if (isSToken && this.STokenFrequency == 0) return - this.Options.IndexSize;
            if (isSToken) return - (this.Options.IndexSize + this.Options.BPC) * this.STokenFrequency;
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Words[word] - 1) - this.Options.IndexSize;
        }

        private byte[] GetHighestRankedWord(ref bool usesSToken)
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

        private void SplitByWord(in byte[] buffer, int wordLength, int wordLocation, bool usesSToken)
        {
            // TODO: Use s token
            // if (usesSToken)
            // {
            //     this.SToken = this.SToken.Concat(buffer).ToArray();
            //     this.STokenFrequency++;
            //     continue;
            // }

            for (int l = 0; l < buffer.Length; l++)
            {
                // Find all locations of this word l
                if (this.Arr[wordLength - 1][l] != wordLocation) continue;

                for (int j = 0; j < buffer.Length; j++)
                {
                    for (int i = 0; i < this.Arr.Length; i++)
                    {
                        // Define start and end of exclusion region
                        int start = l - i;
                        int end = l + wordLength - 1 + i;
                        // Enforce bounds
                        start = start >= 0 ? start : 0;
                        end = end < buffer.Length ? end : buffer.Length - 1;
                        // Mark as unavailable
                        for (int s = start; s <= end; s++)
                            this.Arr[i][s] = -1;
                    }
                }
            }
        }
    }
}