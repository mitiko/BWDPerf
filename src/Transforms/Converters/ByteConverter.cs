using BWDPerf.Interfaces;

namespace BWDPerf.Transforms.Converters
{
    public class ByteConverter : IConverter<byte>
    {
        public int BytesPerSymbol { get; set; } = 1;

        public byte[] Convert(byte symbol) => new byte[] { symbol };

        public byte Convert(byte[] bytes) => bytes[0];
    }
}