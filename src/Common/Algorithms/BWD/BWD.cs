using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BWDPerf.Common.Entities;
using BWDPerf.Interfaces;

namespace BWDPerf.Common.Algorithms.BWD
{
    public class BWD : ICoder<byte[], DictionaryIndex>, ICoder<byte[], DictionaryIndex[]>
    {
        public int MaxSizeWord { get; }
        public Dictionary<string, Word> Words { get; }
        public Word[] Dictionary { get; }
        public int BPC { get; }

        public BWD(int maxSizeWord = 16, int indexSize = 5, int bpc = 8)
        {
            this.MaxSizeWord = 16;
            this.Words = new();
            this.Dictionary = new Word[1 << indexSize];
            this.BPC = bpc;
        }

        async IAsyncEnumerable<DictionaryIndex> ICoder<byte[], DictionaryIndex>.Encode(IAsyncEnumerable<byte[]> input)
        {
            await foreach (var buffer in input)
            {
                GetAllWords(buffer);
                for (int i = 0; i < this.Dictionary.Length; i++)
                {
                    var word = GetHighestRankedWord();
                    var contexts = SplitByWord(buffer, word);
                    if (i != this.Dictionary.Length - 1)
                        SelectWords(contexts);
                }
                // Output dictionary
                // Split by dictionary into index buffer
                // return indices in order or as buffers
                yield return default;
            }
        }

        async IAsyncEnumerable<DictionaryIndex[]> ICoder<byte[], DictionaryIndex[]>.Encode(IAsyncEnumerable<byte[]> input)
        {
            await System.Threading.Tasks.Task.Delay(1);
            throw new NotImplementedException();
        }

        private void Rank(Word word)
        {
            word.Rank = (word.Count * this.BPC - this.Dictionary.Length) * (word.Size - 1);
        }

        void GetAllWords(byte[] buffer)
        {
            int start, end; Word word; string w;
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
    
        private void SelectWords(byte[][] contexts)
        {
            throw new NotImplementedException();
        }

        private byte[][] SplitByWord(byte[] buffer, Word word)
        {
            throw new NotImplementedException();
        }

        private Word GetHighestRankedWord()
        {
            var word = this.Words.First().Value;
            Rank(word);
            foreach (var pair in this.Words)
            {
                Rank(pair.Value);
                if (pair.Value.Rank > word.Rank)
                {
                    word = pair.Value;
                }
            }
            return word;
        }
    }
}