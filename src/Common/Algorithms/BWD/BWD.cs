using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Common.Entities;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Algorithms.BWD
{
    // Encode the buffer and pass it on as individual symbols or as blocks of indices
    public class BWD : ICoder<byte[], DictionaryIndex>, ICoder<byte[], DictionaryIndex[]>
    {
        public int MaxSizeWord { get; } // m
        public Dictionary<string, Word> Words { get; } // W
        public Word[] Dictionary { get; } // dict
        public int BPC { get; } // bpc - bits per character
        public int IndexSize { get; } // log_2(len(dict))
        public bool UseEndPattern { get; private set; }
        public Word EndPattern { get; }

        public BWD(int maxSizeWord = 16, int indexSize = 5, int bpc = 8)
        {
            this.MaxSizeWord = maxSizeWord;
            this.IndexSize = indexSize;
            this.BPC = bpc;
            this.Dictionary = new Word[1 << indexSize]; // len(dict) = 2^m
            this.Words = new();
            this.EndPattern = new Word(new byte[0], 1, true);
        }

        async IAsyncEnumerable<DictionaryIndex> ICoder<byte[], DictionaryIndex>.Encode(IAsyncEnumerable<byte[]> input)
        {
            double savedBits = 0;
            await foreach (var buffer in input)
            {
                Word word; int dictionarySize = 0;
                var contexts = new List<byte[]>() { buffer };
                for (int i = 0; i < this.Dictionary.Length; i++)
                {
                    GetAllWords(contexts);
                    if (this.UseEndPattern && i == this.Dictionary.Length - 1)
                        word = this.EndPattern;
                    else
                        word = GetHighestRankedWord();

                    contexts = SplitByWord(contexts, ref word);
                    this.Dictionary[i] = word;

                    if (i == this.Dictionary.Length - 1 && contexts.Count > 0)
                    {
                        i--;
                        if (!this.UseEndPattern) this.UseEndPattern = true;
                        else throw new ApplicationException($"Not all information was encoded by BWD. Left - {contexts.Sum(x => x.Length)}");
                        this.UseEndPattern = true;
                    }

                    if (contexts.Count == 0)
                    {
                        dictionarySize = i + 1;
                        if (Math.Ceiling(Math.Log2(i+1)) < this.IndexSize)
                            Console.WriteLine($"BWD index was too high. Set to {this.IndexSize}, but completed with {dictionarySize} of {1 << this.IndexSize} words. Try using {Math.Ceiling(Math.Log2(dictionarySize))}");
                        break;
                    }
                }

                if (this.UseEndPattern) Console.WriteLine("Forced to use a pattern to finish compression.");
                else if (this.Dictionary[dictionarySize-1].IsPattern) Console.WriteLine("Decided to use a pattern to minimize loss.");
                Console.WriteLine("Computed optimal dictionary:");
                for (int i = 0; i < dictionarySize; i++)
                {
                    word = this.Dictionary[i];
                    Console.WriteLine($"{word.Size} -- \"{word.ToString()}\" with loss of {Loss(word)} bits");
                    for (int j = 0; j < word.Count; j++)
                    {
                        if (word.IsPattern)
                        {
                            for (int k = 0; k < word.Size; k++)
                                yield return new DictionaryIndex() { Index = i };
                            break;
                        }
                        yield return new DictionaryIndex() { Index = i };
                    }
                }
                savedBits += this.Dictionary.Take(dictionarySize).Select(x => Loss(x)).Sum();
                // Output dictionary
                // Split by dictionary into index buffer
                // return indices in order
                yield return default;
                // break;
            }
            Console.WriteLine($"Saved {savedBits} bits!");
        }

        async IAsyncEnumerable<DictionaryIndex[]> ICoder<byte[], DictionaryIndex[]>.Encode(IAsyncEnumerable<byte[]> input)
        {
            await System.Threading.Tasks.Task.Delay(1);
            yield return default;
            throw new NotImplementedException();
        }

        private void GetAllWords(List<byte[]> contexts)
        {
            this.Words.Clear();
            int start, end; Word word; string w;
            foreach (var buffer in contexts)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    start = i;
                    for (int j = 1; j <= this.MaxSizeWord; j++)
                    {
                        end = start + j;
                        if (end > buffer.Length) break;

                        word = new Word(buffer[start..end], 1);
                        w = word.ToString();
                        if (this.Words.ContainsKey(w))
                        {
                            word.Count = this.Words[w].Count + 1;
                            this.Words[w] = word;
                        }
                        else
                        {
                            this.Words.Add(w, word);
                        }
                    }
                }
            }
        }
    
        private double Rank(Word word)
        {
            return (word.Size * this.BPC - this.IndexSize) * (word.Count - 1);
        }

        private double Loss(Word word)
        {
            return (word.Size * this.BPC - this.IndexSize) * (word.Count - 1) - this.IndexSize;
        }

        private Word GetHighestRankedWord()
        {
            var word = this.Words.First().Value;
            double rank, newRank;
            rank = Rank(word);
            foreach (var pair in this.Words)
            {
                newRank = Rank(pair.Value);
                if (newRank > rank)
                {
                    word = pair.Value;
                    rank = newRank;
                }
                if (newRank == rank)
                {
                    if (pair.Value.Count > word.Count) word = pair.Value;
                    if (pair.Value.Count == word.Count)
                        if (pair.Value.Size > word.Size)
                            word = pair.Value;
                }
            }
            if (Loss(word) <= 0) // This is without ranking patterns, but uses them
                return this.EndPattern;
            return word;
        }

        private List<byte[]> SplitByWord(List<byte[]> contexts, ref Word word)
        {
            var result = new List<byte[]>();
            int start, count;
            var w = word.Content;
            foreach (var buffer in contexts)
            {
                start = 0;
                count = 0;
                if (word.IsPattern)
                {
                    var buff = new byte[word.Size + buffer.Length];
                    for (int i = 0; i < buff.Length; i++)
                        buff[i] = i < word.Size ? word.Content[i] : buffer[i - word.Size];
                    word.Content = buff;
                    continue;
                }
                for (int i = 0; i < buffer.Length; i++)
                {
                    count = w[count] == buffer[i] ? count + 1 : 0;
                    // If match or end of buffer
                    if (count == w.Length || i == buffer.Length - 1)
                    {
                        var buff = buffer[start..(i-count+1)];
                        if (buff.Length > 0) result.Add(buff);
                        count = 0; start = i+1;
                    }
                }
            }
            return result;
        }
    }
}