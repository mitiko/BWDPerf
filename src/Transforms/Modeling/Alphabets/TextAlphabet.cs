using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Alphabets
{
    public class TextAlphabet : IAlphabet<byte>
    {
        private byte[] Symbols { get; }
        public byte this[int index] => this.Symbols[index];
        public int this[byte symbol] => symbol;
        public int Length => this.Symbols.Length;

        public TextAlphabet()
        {
            this.Symbols = new byte[256];
            for (int i = 0; i < 256; i++)
                this.Symbols[i] = (byte) i;
        }
    }
}