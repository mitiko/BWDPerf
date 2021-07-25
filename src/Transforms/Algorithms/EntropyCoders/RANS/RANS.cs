using System;
using System.Collections.Generic;
using BWDPerf.Transforms.Algorithms.EntropyCoders.RANS.Entities;

namespace BWDPerf.Transforms.Algorithms.EntropyCoders.RANS
{
    internal static class RANS
    {
        // Normalization range is [L, bL), where L = kM to ensure b-uniqueness
        // M is the denominator = sum_s (f_s)
        // log2(M) is the accuracy of the quantizer (in bits)
        // log2(b) is how many bits at a time we write to the stream.
        // This implementation is byte aligned, so b = 256
        // L is 1<<23, so bL = (1<<23)*256 = 1<<31 < uint.MaxValue
        // The higher L is the more accurate the coding is
        public const uint _L = 1u << 23;
        public const uint _bMask = 255; // mask to get the last logB bits
        public const int _logB = 8; // b = 256, so we emit a byte when normalizing

        public static void Encode(ref uint state, RANSSymbol symbol, int logM)
            => state = ((state / symbol.Freq) << logM) + state % symbol.Freq + symbol.Cdf;

        public static void Decode(ref uint state, RANSSymbol symbol, int logM, uint mask) =>
            state = symbol.Freq * (state >> logM) + (state & mask) - symbol.Cdf;

        public static void RenormalizeEncode(ref uint state, RANSSymbol symbol, int logM, Stack<byte> stream)
        {
            // Read bytes out of the state
            uint state_max = ((_L << _logB) >> logM) * symbol.Freq;
            while(state >= state_max)
            {
                stream.Push((byte) (state & _bMask));
                state >>= _logB;
            }
        }

        // Returns whether or not we should stop decoding
        public static bool RenormalizeDecode(ref uint state, Queue<byte> byteQueue)
        {
            // Read bytes into the state
            if (byteQueue.Count < 1 && state <= _L) return true;
            while (state < _L)
                state = (state << _logB) | byteQueue.Dequeue();
            return false;
        }
    }
}