using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Converters
{
    public class NibbleConverter : IConverter<byte, byte>, IConverter<byte, ushort>
    {
        public Queue<byte> Queue { get; set; } = new();

        void IConverter<byte, byte>.Buffer(byte predictionSymbol) => this.Queue.Enqueue(predictionSymbol);
        void IConverter<byte, ushort>.Buffer(byte predictionSymbol) => this.Queue.Enqueue(predictionSymbol);

        IEnumerable<byte> IConverter<byte, byte>.Convert()
        {
            while (this.Queue.Count >= 2)
            {
                var highNibble = Queue.Dequeue();
                var lowNibble = Queue.Dequeue();
                yield return (byte) ((highNibble << 4) | lowNibble);
            }
        }

        IEnumerable<ushort> IConverter<byte, ushort>.Convert()
        {
            while (this.Queue.Count >= 4)
            {
                var nibble0 = Queue.Dequeue();
                var nibble1 = Queue.Dequeue();
                var nibble2 = Queue.Dequeue();
                var nibble3 = Queue.Dequeue();
                yield return (ushort) (
                    (nibble0 << 12) | (nibble1 << 8) |
                    (nibble2 << 4)  | nibble3
                );
            }
        }

        bool IConverter<byte, byte>.Flush() => this.Queue.Count != 0;
        bool IConverter<byte, ushort>.Flush() => this.Queue.Count != 0;
    }
}