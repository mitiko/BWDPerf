using System;
using System.Collections.Generic;
using System.Text;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Tools
{
    public class CountWord : ICoder<byte, byte>
    {
        private byte[] BytesToMatch { get; }
        public string Word { get; }
        public int Count { get; private set; }

        public CountWord(string word, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            this.BytesToMatch = encoding.GetBytes(word);
            this.Word = word;
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<byte> input)
        {
            int index = 0;
            await foreach (var symbol in input)
            {
                if (index == this.BytesToMatch.Length)
                {
                    this.Count++;
                    index = 0;
                }
                index = this.BytesToMatch[index] == symbol ? index + 1 : 0;

                yield return symbol;
            }

            Console.WriteLine($"[{this.GetHashCode()}] The word \"{this.Word}\" was counted {this.Count} times");
        }
    }
}