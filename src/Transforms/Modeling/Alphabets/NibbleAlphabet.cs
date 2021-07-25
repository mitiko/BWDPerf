using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Alphabets
{
    public class NibbleAlphabet : IAlphabet<byte>
    {
        private byte[] Symbols { get; }
        public byte this[int index] => this.Symbols[index];
        public int this[byte symbol] => symbol;
        public int Length => this.Symbols.Length;

        public NibbleAlphabet()
        {
            this.Symbols = new byte[16];
            for (int i = 0; i < 16; i++)
                this.Symbols[i] = (byte) i;
        }
    }
}