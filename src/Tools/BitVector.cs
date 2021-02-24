namespace BWDPerf.Tools
{
    public struct BitVector
    {
        public int Length { get; }
        private uint[] Array { get; }
        public bool this[int index]
        {
            get
            {
                if (index >= this.Length) throw new System.IndexOutOfRangeException();
                return (this.Array[index >> 5] & (1 << (31 - index & 31))) != 0;
            }
            set
            {
                var bit = this[index];
                if (bit == value) return;
                int pos = 31 - index & 31;
                if (bit == true)
                    this.Array[index >> 5] -= (uint) (1 << pos);
                else
                    this.Array[index >> 5] += (uint) (1 << pos);
            }
        }

        public BitVector(int length)
        {
            this.Length = length;
            this.Array = new uint[(length >> 5) + ((length & 31) == 0 ? 0 : 1)];
        }
    }
}