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
        // public Dictionary<int, int> Arr { get; set; }
        public byte[] SToken { get; set; }
        public int STokenFrequency { get; set; }

        public BWD(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.SToken = new byte[0];
            this.STokenFrequency = 0;
        }

        public async IAsyncEnumerable<DictionaryIndex[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            int k = 0;
            int totalSaved = 0;
            await foreach (var buffer in input)
            {
                var (usesSToken, dictionarySize, savedBits) = CalculateDictionary(buffer);
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

        private (bool usesSToken, int dictionarySize, int savedBits) CalculateDictionary(byte[] buffer)
        {
            var timer = Stopwatch.StartNew();
            // The initial context is the whole buffer
            var contexts = new List<byte[]>() { buffer };
            bool usesSToken = false; int dictionarySize = 0;
            // Initialize words -> O(m^3 * (b/m - 2/3))
            GetAllWords(buffer);
            Console.WriteLine($"First taking of words took: {timer.Elapsed}");
            timer.Restart();
            int savedBits = 0;
            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                // Get the best word
                var word = GetHighestRankedWord(ref usesSToken);
                Console.WriteLine($"Getting highest ranked word took: {timer.Elapsed}");
                timer.Restart();
                // Split by word and save it to dictionary
                contexts = SplitByWord(contexts, word, usesSToken);
                Console.WriteLine($"Splitting took: {timer.Elapsed}");
                timer.Restart();
                this.Dictionary[i] = word;
                // this.Freq[word] = usesSToken ? this.STokenFrequency : this.Words[word];
                savedBits += Loss(word, usesSToken);

                // If reached the end of the dictionary and more data is left,
                // re-run the last iteration with the SToken
                Console.WriteLine($"{i} --- {Loss(word, usesSToken)} --- \"{Print(word, usesSToken)}\"");
                if (i == this.Dictionary.Length - 1 && contexts.Count > 0)
                { i--; usesSToken = true; } // TODO: Changes the contexts and then the s token is applied, making decompression impossible

                // If we've got no data left to encode, save dictionary size
                if (contexts.Count == 0)
                { dictionarySize = i + 1; break; }

                // Select words for the next iteration
                // SelectWords(contexts, word);
                SelectWords(contexts);
                Console.WriteLine($"Seconf selection took {timer.Elapsed}");
                timer.Restart();
            }

            return (usesSToken, dictionarySize, savedBits);
        }

        private string Print(byte[] word, bool isSToken)
        {
            if (isSToken) return "<s>";
            string str = "";
            foreach (var s in word)
            {
                str += (char) s;
            }
            return str;
        }

        private void GetAllWords(byte[] buffer)
        {
            this.Words = new(new ByteArrayComparer());
            int start, end; byte[] word;

            for (int i = 0; i < buffer.Length; i++)
            {
                start = i;
                for (int j = 1; j <= this.Options.MaxWordSize; j++)
                {
                    end = start + j;
                    if (end > buffer.Length) break;

                    word = buffer[start..end];
                    if (this.Words.ContainsKey(word)) this.Words[word] += 1;
                    else this.Words.Add(word, 1);
                }
            }
        }

        private void SelectWords(List<byte[]> contexts)
        {
            this.Words = new(new ByteArrayComparer());
            int start, end; byte[] word;

            foreach (var buffer in contexts)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    start = i;
                    for (int j = 1; j <= this.Options.MaxWordSize; j++)
                    {
                        end = start + j;
                        if (end > buffer.Length) break;

                        word = buffer[start..end];
                        if (this.Words.ContainsKey(word)) this.Words[word] += 1;
                        else this.Words.Add(word, 1);
                    }
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

        private List<byte[]> SplitByWord(List<byte[]> contexts, byte[] word, bool usesSToken)
        {
            var result = new List<byte[]>();
            foreach (var buffer in contexts)
            {
                if (usesSToken)
                {
                    this.SToken = this.SToken.Concat(buffer).ToArray();
                    this.STokenFrequency++;
                    continue;
                }

                int start = 0, count = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    count = word[count] == buffer[i] ? count + 1 : 0;
                    // If match
                    if (count == word.Length)
                    {
                        var buff = buffer[start..(i-count+1)];
                        if (buff.Length > 0) result.Add(buff);
                        start = i+1; count = 0; continue;
                    }

                    // If end of buffer
                    if (i == buffer.Length - 1)
                    {
                        result.Add(buffer[start..]);
                    }
                }
            }
            return result;
        }
    }
}