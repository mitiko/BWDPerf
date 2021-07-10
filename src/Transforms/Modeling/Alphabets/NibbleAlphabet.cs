using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Modeling.Alphabets
{
    public class NibbleAlphabet : INibbleAlphabet<byte>
    {
        public (int, int)[] SN { get; } // symbols to nibbles table
        public byte[][] NS { get; } // nibbles to symbol table

        public byte this[(int s1, int s2) index] => this.NS[index.s1][index.s2];
        public (int, int) this[byte symbol] => this.SN[symbol];
        public int Length => 16;

        public NibbleAlphabet()
        {
            this.SN = new (int, int)[256];
            this.NS = new byte[16][];

            for (int i = 0; i < 256; i++)
                this.SN[i] = ((i & 240) >> 4, i & 15);

            for (int s1 = 0; s1 < 16; s1++)
            {
                this.NS[s1] = new byte[16];
                for (int s2 = 0; s2 < 16; s2++)
                    this.NS[s1][s2] = (byte) ((s1 << 4) | s2);
            }
        }
    }
}