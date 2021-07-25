using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Converters
{
    public class IdentityConverter : IConverter<byte, byte>, IConverter<ushort, ushort>
    {
        public byte Int8Buffer { get; set; }
        public ushort Int16Buffer { get; set; }

        void IConverter<byte, byte>.Buffer(byte predictionSymbol) => this.Int8Buffer = predictionSymbol;
        void IConverter<ushort, ushort>.Buffer(ushort predictionSymbol) => this.Int16Buffer = predictionSymbol;

        IEnumerable<byte> IConverter<byte, byte>.Convert() { yield return this.Int8Buffer; }
        IEnumerable<ushort> IConverter<ushort, ushort>.Convert() { yield return this.Int16Buffer; }

        bool IConverter<byte, byte>.Flush() => false;
        bool IConverter<ushort, ushort>.Flush() => false;
    }
}