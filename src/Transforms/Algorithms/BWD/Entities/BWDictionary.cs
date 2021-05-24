using System;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDictionary : Dictionary<ushort, ReadOnlyMemory<byte>>
    {
        public ReadOnlyMemory<byte> Serialize()
        {
            var buffer = new List<byte>(capacity: this.Count * 3);
            // TODO: This can be 2 bytes for ushort
            buffer.AddRange(BitConverter.GetBytes(this.Count));
            for (ushort i = 0; i < this.Count; i++)
            {
                buffer.Add((byte) this[i].Length);

                foreach (var symbol in this[i].Span)
                    buffer.Add(symbol);
            }
            return buffer.ToArray();
        }
    }
}