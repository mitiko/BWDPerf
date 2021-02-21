using System;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDictionary
    {
        private ReadOnlyMemory<byte>[] Dictionary { get; }
        public int IndexSize { get; }
        public int Length => this.Dictionary.Length;
        public int STokenIndex => this.Dictionary.Length - 1;
        public ReadOnlyMemory<byte> SToken => this.Dictionary[this.STokenIndex];
        private bool wordCountChanged = true;
        private int wordCount = 0;
        public ReadOnlyMemory<byte> this[int index]
        {
            get
            {
                return this.Dictionary[index];
            }
            set
            {
                wordCountChanged = true;
                this.Dictionary[index] = value;
            }
        }
        public int WordCount
        {
            get
            {
                if (!wordCountChanged) return wordCount;
                wordCount = 0;
                for (int i = 0; i < this.Dictionary.Length && this.Dictionary[i].Length > 0; i++)
                    wordCount++;
                return wordCount;
            }
        }

        public BWDictionary(int indexSize)
        {
            this.Dictionary = new ReadOnlyMemory<byte>[1 << indexSize];
            this.IndexSize = indexSize;
        }

        public ReadOnlyMemory<byte> Serialize()
        {
            // Precalculate total size so we don't resize the list.
            int size = 4; // 4 bytes for the dictionary size
            for (int i = 0; i < this.WordCount; i++)
                size += this.Dictionary[i].Length;

            var buffer = new List<byte>(capacity: size);
            buffer.AddRange(BitConverter.GetBytes(this.WordCount));
            for (int i = 0; i < this.WordCount; i++)
            {
                if (i == this.STokenIndex)
                    buffer.AddRange(BitConverter.GetBytes(this.Dictionary[i].Length));
                else
                    buffer.Add((byte) this.Dictionary[i].Length);

                foreach (var symbol in this.Dictionary[i].Span)
                    buffer.Add(symbol);
            }
            return buffer.ToArray();
        }
    }
}