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
        public OccurenceDictionary<Word> Count { get; set; }
        public int[][] WordRef { get; set; }
        public byte[] STokenData { get; set; }
        public Word SToken { get; set; }

        public BWD(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.WordRef = new int[options.IndexSize][];
            // This is a measure of repating count. The actual real count is always with one more.
            this.Count = new OccurenceDictionary<Word>();
            this.STokenData = new byte[0];
            this.SToken = new Word(-1, 0);
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
                // TODO: seperate in new method
                var fileWriter = new BinaryWriter(new FileInfo($"dictionary-{k}-{this.GetHashCode()}.bwd.dict").OpenWrite());
                fileWriter.Write(dictionarySize);
                for (int i = 0; i < dictionarySize; i++)
                {
                    var word = this.Dictionary[i];
                    if (i == dictionarySize-1)
                    {
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        fileWriter.Write(this.STokenData.Length);
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        fileWriter.Write('7');
                        // foreach (var character in this.STokenData) fileWriter.Write(character);
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
                    dict[i] = new DictionaryIndex(i, this.Options.IndexSize);
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
            CountWords(in buffer); // Count the matching words
            Console.WriteLine($"Counting the words took: {timer.Elapsed}");
            timer.Restart();

            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                bool isLastWord = i == this.Dictionary.Length - 1;
                Word word;
                if (isLastWord)
                {
                    CollectSTokenData(in buffer);
                    this.Dictionary[i] = this.STokenData;
                    expectedSavedBits += Loss(this.SToken, isLastWord);
                    Console.WriteLine($"{i} --- {Loss(this.SToken, isLastWord)} --- \"{Print(this.Dictionary[i], isLastWord)}\"");
                    dictionarySize = i + 1;
                    break;
                }

                // Get the best word
                word = GetHighestRankedWord();
                Console.WriteLine($"Getting highest ranked word took: {timer.Elapsed}");
                timer.Restart();

                // Save the chosen word
                this.Dictionary[i] = isLastWord ? this.STokenData : buffer[word.Location..(word.Location + word.Length)];
                // Calculate estimated savings
                expectedSavedBits += Loss(word, isLastWord);
                Console.WriteLine($"{i} --- {Loss(word, isLastWord)} --- \"{Print(this.Dictionary[i], isLastWord)}\"");

                // Split by word and save it to dictionary
                SplitByWord(in buffer, word);
                Console.WriteLine($"Splitting took: {timer.Elapsed}");
                timer.Restart();
                // Count new occurences
                CountWords(in buffer);
                Console.WriteLine($"Counting the words took: {timer.Elapsed}");
                timer.Restart();

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
            {
                this.WordRef[i] = new int[buffer.Length - i];
                for (int j = 0; j < this.WordRef[i].Length; j++)
                    this.WordRef[i][j] = -1;
            }

            for (int i = 0; i < this.WordRef.Length; i++)
            {
                Console.WriteLine($"i = {i}");
                for (int j = 0; j < this.WordRef[i].Length; j++)
                {
                    if (this.WordRef[i][j] != -1) continue;
                    this.WordRef[i][j] = j;

                    if (i == 0)
                    {
                        for (int index = j + 1; index < this.WordRef[i].Length; index++)
                            if (buffer[j] == buffer[index]) this.WordRef[i][index] = j;
                        continue;
                    }

                    byte[] selection = new byte[i + 1];
                    int l = this.WordRef[0][j];
                    for (int index = j + 1; index < this.WordRef[i].Length;)
                    {
                        if (this.WordRef[0][index] != l) { index++; continue; } // check if first character matches or don't waste my time and space

                        selection = buffer[index..(index + i + 1)];
                        bool match = true;
                        for (int s = 0; s < selection.Length; s++)
                            if (buffer[j + s] != selection[s]) { match = false; break; }

                        if (match == true) { this.WordRef[i][index] = j; index += (i + 1); }
                        else { index++; }
                    }
                }
            }
        }

        private int Rank(Word word)
        {
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Count[word] - 1);
        }

        private int Loss(Word word, bool isLastWord = false)
        {
            if (isLastWord && this.Count[word] == 0) return - this.Options.IndexSize;
            if (isLastWord) return - (this.Options.IndexSize + this.Options.BPC) * this.Count[word];
            return (word.Length * this.Options.BPC - this.Options.IndexSize) * (this.Count[word] - 1) - this.Options.IndexSize;
        }

        private Word GetHighestRankedWord()
        {
            var bestWord = this.Count.Keys.First();
            int rank, newRank;
            rank = Rank(bestWord);
            foreach (var word in this.Count.Keys)
            {
                newRank = Rank(word);
                if (newRank > rank) { bestWord = word; rank = newRank; }
                // TODO: Make considerations on the contexts from which the words were taken
                if (newRank == rank && this.Count[word] >= this.Count[bestWord]) { bestWord = word; }
            }

            return bestWord;
        }

        private void SplitByWord(in byte[] buffer, Word word)
        {
            var locations = new int[this.Count[word]]; int x = 0;
            for (int j = 0; j < buffer.Length; j++)
            {
                // Find all locations of this word l
                if (j >= this.WordRef[word.Length - 1].Length) break;
                if (this.WordRef[word.Length - 1][j] != word.Location) continue;
                locations[x++] = j;
            }

            for (int l = 0; l < locations.Length; l++)
            {
                for (int i = 0; i < this.WordRef.Length; i++)
                {
                    // Define start and end of exclusion region
                    int start = locations[l] - i;
                    int end = locations[l] + word.Length - 1 + i;
                    // Enforce bounds
                    start = start >= 0 ? start : 0;
                    end = end < this.WordRef[i].Length ? end : this.WordRef[i].Length - 1;
                    // Mark as unavailable
                    for (int s = start; s <= end; s++)
                        this.WordRef[i][s] = -1;
                }
            }
        }

        private void CountWords(in byte[] buffer)
        {
            this.Count.Clear();
            for (int i = 0; i < this.WordRef.Length; i++)
            {
                for (int j = 0; j < this.WordRef[i].Length; j++)
                {
                    if (this.WordRef[i][j] != -1)
                        this.Count.Add(new Word(this.WordRef[i][j], i + 1));
                }
            }
        }

        private void CollectSTokenData(in byte[] buffer)
        {
            var list = new List<byte>();
            bool isNewToken = true;
            for (int j = 0; j < buffer.Length; j++)
            {
                if (this.WordRef[0][j] != -1)
                {
                    list.Add(buffer[j]);
                    if (isNewToken == true)
                    {
                        this.Count.Add(this.SToken);
                        isNewToken = false;
                    }
                }
                else { isNewToken = true; }
            }
            this.STokenData = list.ToArray();
            return;
        }
    }
}