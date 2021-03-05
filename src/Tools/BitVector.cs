namespace BWDPerf.Tools
{
    public class BitVector
    {
        public int Length { get; }
        private uint[] Array { get; }
        public bool this[int index]
        {
            get
            {
                if (index >= this.Length) throw new System.IndexOutOfRangeException();
                return ((this.Array[index >> 5] >> (index & 31)) & 1) != 0;
            }
            set
            {
                if (index >= this.Length) throw new System.IndexOutOfRangeException();
                int pos = index & 31;
                if (value)
                    this.Array[index >> 5] |= (1u << pos);
                else
                    this.Array[index >> 5] &= uint.MaxValue ^ (1u << pos);
            }
        }

        public BitVector(int length, bool bit = false)
        {
            this.Length = length;
            var lastIsPartial = (length & 31) != 0;
            this.Array = new uint[(length >> 5) + (lastIsPartial ? 1 : 0)];
            if (bit)
            {
                for (int i = 0; i < this.Array.Length - (lastIsPartial ? 1 : 0); i++)
                    this.Array[i] = uint.MaxValue;
                if (lastIsPartial)
                    this.Array[this.Array.Length - 1] = (1u << (length & 31)) - 1;
            }
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < this.Array.Length; i++)
                if (this.Array[i] != 0) return false;

            return true;
        }

        public int Rank(bool bit)
        {
            throw new System.NotImplementedException();
        }
    }
}