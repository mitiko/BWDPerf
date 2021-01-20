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
    public class BWDEncoder : ICoder<byte[], (byte[], DictionaryIndex[])>
    {
        public Options Options { get; set; }
        public byte[][] Dictionary { get; }
        public byte[] STokenData { get; set; }
        public Word SToken { get; set; }

        public BWDEncoder(Options options)
        {
            this.Options = options;
            this.Dictionary = new byte[1 << options.IndexSize][]; // len(dict) = 2^m
            this.STokenData = new byte[0];
            this.SToken = new Word(-1, 0);
        }

        public async IAsyncEnumerable<(byte[], DictionaryIndex[])> Encode(IAsyncEnumerable<byte[]> input)
        {
            await foreach (var buffer in input)
            {
                var dictionarySize = CalculateDictionary(in buffer);
                byte[] dictionary = EncodeDictionary(dictionarySize);
                DictionaryIndex[] stream = EncodeStream(in buffer, dictionarySize);

                yield return (dictionary, stream);
            }
        }

        private int CalculateDictionary(in byte[] buffer)
        {
            int dictionarySize = 0;
            int[][] wordRef = new int[this.Options.MaxWordSize][];
            var wordCount = new OccurenceDictionary<Word>();

            var timer = Stopwatch.StartNew();
            FindAllMatchingWords(in buffer, ref wordRef); // Initialize words -> O(mb^2)
            Console.WriteLine($"Finding all matching words took: {timer.Elapsed}");
            timer.Restart();
            CountWords(in buffer, ref wordRef, ref wordCount); // Count the matching words
            Console.WriteLine($"Counting the words took: {timer.Elapsed}");
            timer.Restart();

            for (int i = 0; i < this.Dictionary.Length; i++)
            {
                bool isLastWord = i == this.Dictionary.Length - 1;
                Word word;
                if (isLastWord)
                {
                    CollectSTokenData(in buffer, ref wordRef, ref wordCount);
                    this.Dictionary[i] = this.STokenData;
                    Console.WriteLine($"{i} --- {- (this.Options.IndexSize + this.Options.BPC) * wordCount[this.SToken]} --- \"<s>\"");
                    dictionarySize = i + 1;
                    break;
                }

                // Get the best word
                word = GetHighestRankedWord(ref wordCount);
                Console.WriteLine($"Getting highest ranked word took: {timer.Elapsed}");
                timer.Restart();

                // Save the chosen word
                this.Dictionary[i] = isLastWord ? this.STokenData : buffer[word.Location..(word.Location + word.Length)];
                Console.WriteLine($"{i} --- {Rank(word, ref wordCount).ToString("0.00")} --- \"{Print(this.Dictionary[i], isLastWord)}\"");

                // Split by word and save it to dictionary
                SplitByWord(in buffer, word, ref wordRef, ref wordCount);
                Console.WriteLine($"Splitting took: {timer.Elapsed}");
                timer.Restart();
                // Count new occurences
                CountWords(in buffer, ref wordRef, ref wordCount);
                Console.WriteLine($"Counting the words took: {timer.Elapsed}");
                timer.Restart();

                // When all references have been encoded, save the dictionary size and exit
                if (wordCount.Values.Sum() == 0)
                { dictionarySize = i + 1; break; }
            }

            return dictionarySize;
        }

        private double Rank(Word word, ref OccurenceDictionary<Word> wordCount)
        {
            var c = wordCount[word]; // Count of this word
            var l = word.Length;
            return (l * this.Options.BPC - this.Options.IndexSize) * (c - 1);
        }

        private void FindAllMatchingWords(in byte[] buffer, ref int[][] wordRef)
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

        private void CountWords(in byte[] buffer, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
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

        private Word GetHighestRankedWord(ref OccurenceDictionary<Word> wordCount)
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

        private void SplitByWord(in byte[] buffer, Word word, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
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

        private void CollectSTokenData(in byte[] buffer, ref int[][] wordRef, ref OccurenceDictionary<Word> wordCount)
        {
            var list = new List<byte>();
            bool isNewToken = false;
            for (int j = 0; j < buffer.Length; j++)
            {
                if (wordRef[0][j] != -1)
                {
                    if (isNewToken == true)
                    {
                        list.Add(0xff);
                        wordCount.Add(this.SToken);
                        isNewToken = false;
                    }

                    list.Add(buffer[j]);
                    if (buffer[j] == 0xff) list.Add(0xff);
                }
                else { isNewToken = true; }
            }
            this.STokenData = list.ToArray();
            return;
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

        private byte[] EncodeDictionary(int dictionarySize)
        {
            var buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(dictionarySize));
            for (int i = 0; i < dictionarySize; i++)
            {
                if (i == dictionarySize - 1 && this.STokenData.Length > 0)
                    buffer.AddRange(BitConverter.GetBytes(this.STokenData.Length));
                else
                    buffer.Add((byte) this.Dictionary[i].Length);

                foreach (var symbol in this.Dictionary[i])
                    buffer.Add(symbol);
            }
            for (int i = 0; i < 64; i++)
            {
                buffer.Add((byte) '#');
            }
            return buffer.ToArray();
        }

        private DictionaryIndex[] EncodeStream(in byte[] buffer, int dictionarySize)
        {
            // TODO: find which index this should be
            var stoken = new DictionaryIndex(this.Dictionary.Length - 1);
            var data = new int[buffer.Length];
            int wordCount = 0;
            for (int k = 0; k < data.Length; k++)
                data[k] = stoken.Index; // Initialize with <s> token

            for (int i = 0; i < dictionarySize; i++)
            {
                if (i == stoken.Index) break; // Don't do this for <s> tokens, they'll be what's left behind
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
                        wordCount++;
                        for (int k = 0; k < word.Length; k++)
                            data[j+k] = i;
                    }
                }
            }

            var stream = new List<DictionaryIndex>(capacity: wordCount);
            for (int k = 0; k < data.Length;)
            {
                stream.Add(new DictionaryIndex((int) data[k]));
                int offset;
                if (data[k] == this.Dictionary.Length)
                    for (offset = 0; k+offset >= data.Length ? false : data[k] == data[k+offset]; offset++);
                else
                    offset = this.Dictionary[data[k]].Length;
                k += offset;
            }

            return stream.ToArray();
        }
    }
}