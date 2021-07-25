using System;
using System.Collections.Generic;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Converters
{
    public class NibbleBlockConverter : IBlockConverter<byte, byte>, IBlockConverter<ushort, byte>
    {
        public ReadOnlyMemory<byte> Int8Block { get; set; } = null;
        public ReadOnlyMemory<ushort> Int16Block { get; set; } = null;

        public void Load(ReadOnlyMemory<byte> block) => this.Int8Block = block;
        public void Load(ReadOnlyMemory<ushort> block) => this.Int16Block = block;

        int IBlockConverter<byte, byte>.GetConvertedLength() => this.Int8Block.Length * 2;
        int IBlockConverter<ushort, byte>.GetConvertedLength() => this.Int16Block.Length * 4;

        IEnumerable<byte> IBlockConverter<byte, byte>.Convert()
        {
            for (int i = 0; i < this.Int8Block.Length; i++)
            {
                var word = this.Int8Block.Span[i];
                yield return (byte) ((word >> 4) & 15);
                yield return (byte) (word & 15);
            }
        }

        IEnumerable<byte> IBlockConverter<ushort, byte>.Convert()
        {
            for (int i = 0; i < this.Int16Block.Length; i++)
            {
                var word = this.Int16Block.Span[i];
                yield return (byte) ((word >> 12) & 15);
                yield return (byte) ((word >> 8) & 15);
                yield return (byte) ((word >> 4) & 15);
                yield return (byte) (word & 15);
            }
        }
    }
}