using System;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDictionary : Dictionary<int, ReadOnlyMemory<byte>>
    {
        public ReadOnlyMemory<byte> Serialize()
        {
            var buffer = new List<byte>(capacity: this.Count * 3);
            buffer.AddRange(BitConverter.GetBytes(this.Count));
            for (int i = 0; i < this.Count; i++)
            {
                buffer.Add((byte) this[i].Length);

                foreach (var symbol in this[i].Span)
                    buffer.Add(symbol);
            }
            return buffer.ToArray();
        }
    }
}