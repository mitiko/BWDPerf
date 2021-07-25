using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Converters
{
    public class IdentityBlockConverter : IBlockConverter<byte, byte>, IBlockConverter<ushort, ushort>
    {
        public ReadOnlyMemory<byte> Int8Block { get; set; } = null;
        public ReadOnlyMemory<ushort> Int16Block { get; set; } = null;

        void IBlockConverter<byte, byte>.Load(ReadOnlyMemory<byte> block) => this.Int8Block = block;
        void IBlockConverter<ushort, ushort>.Load(ReadOnlyMemory<ushort> block) => this.Int16Block = block;

        int IBlockConverter<byte, byte>.GetConvertedLength() => this.Int8Block.Length;
        int IBlockConverter<ushort, ushort>.GetConvertedLength() => this.Int16Block.Length;

        IEnumerable<byte> IBlockConverter<byte, byte>.Convert() => MemoryMarshal.ToEnumerable(this.Int8Block);
        IEnumerable<ushort> IBlockConverter<ushort, ushort>.Convert() => MemoryMarshal.ToEnumerable(this.Int16Block);
    }
}