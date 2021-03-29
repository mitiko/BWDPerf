using System;
using BWDPerf.Tools;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDIndex
    {
        public SuffixArray SA { get; }
        public BitVector BitVector { get; }
        public ReadOnlyMemory<byte> Buffer { get; }
        public LCPArray LCP { get; set; }

        public BWDIndex(ReadOnlyMemory<byte> buffer)
        {
                var timer = System.Diagnostics.Stopwatch.StartNew();
            this.SA = new SuffixArray(buffer);
                Console.WriteLine($"Suffix array took: {timer.Elapsed}"); timer.Restart();
            this.LCP = new LCPArray(buffer, this.SA);
                Console.WriteLine($"LCP array took: {timer.Elapsed}"); timer.Stop();
            this.BitVector = new BitVector(buffer.Length, bit: true);
            this.Buffer = buffer;
        }

        public int GetParsedCount(Match match)
        {
            // TODO: return parsed count of locations
            throw new NotImplementedException();
        }

        public int[] Parse(Match match)
        {
            // TODO: return parsed locations
            throw new NotImplementedException();
        }
    }
}