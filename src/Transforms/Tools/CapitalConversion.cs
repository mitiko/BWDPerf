using BWDPerf.Interfaces;
using System.Text;
using System.Collections.Generic;

namespace BWDPerf.Transforms.Tools
{
    public class CapitalConversion : ICoder<byte, byte>, ICoder<byte[], byte[]>, IDecoder<byte, byte>
    {
        public Decoder Decoder { get; }
        public Encoder Encoder { get; }
        public int MaxByteCount { get; }
        private readonly byte _flag = (byte) '^'; // 0x5e CIRCUMFLEX ACCENT is 1 byte in UTF8, since it's in ASCII

        public CapitalConversion(Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            this.Decoder = encoding.GetDecoder();
            this.Encoder = encoding.GetEncoder();
            this.MaxByteCount = encoding.GetMaxByteCount(1);
        }

        public async IAsyncEnumerable<byte> Encode(IAsyncEnumerable<byte> input)
        {
            byte[] buffer = new byte[1];
            byte[] lowerCaseBytes = new byte[this.MaxByteCount];
            char[] charBuffer = new char[this.MaxByteCount];
            var skippedBytes = new Queue<byte>();
            await foreach (var symbol in input)
            {
                buffer[0] = symbol;
                var written = this.Decoder.GetChars(buffer, 0, 1, charBuffer, 0);
                skippedBytes.Enqueue(symbol);
                if (written == 0)
                    continue;
                
                for (int i = 0; i < written; i++)
                {
                    if (char.IsUpper(charBuffer[i]))
                    {
                        yield return this._flag;
                        charBuffer[i] = char.ToLower(charBuffer[i]);
                        var bytesCount = this.Encoder.GetBytes(charBuffer, 0, 1, lowerCaseBytes, 0, false);
                        for (int j = 0; j < bytesCount; j++)
                            yield return lowerCaseBytes[j];
                        skippedBytes.Clear();
                    }
                    else
                    {
                        while (skippedBytes.Count != 0)
                        {
                            var s = skippedBytes.Dequeue();
                            if (s == this._flag)
                                yield return this._flag;
                            yield return s;
                        }
                        break;
                    }
                }
            }
            this.Decoder.Reset();
            this.Encoder.Reset();
        }

        public async IAsyncEnumerable<byte[]> Encode(IAsyncEnumerable<byte[]> input)
        {
            byte[] buffer = new byte[1];
            byte[] lowerCaseBytes = new byte[this.MaxByteCount];
            char[] charBuffer = new char[this.MaxByteCount];
            var skippedBytes = new Queue<byte>();
            await foreach (var block in input)
            {
                var list = new List<byte>();
                foreach (var symbol in block)
                {
                    buffer[0] = symbol;
                    var written = this.Decoder.GetChars(buffer, 0, 1, charBuffer, 0);
                    skippedBytes.Enqueue(symbol);
                    if (written == 0)
                        continue;
                    
                    for (int i = 0; i < written; i++)
                    {
                        if (char.IsUpper(charBuffer[i]))
                        {
                            list.Add(this._flag);
                            charBuffer[i] = char.ToLower(charBuffer[i]);
                            var bytesCount = this.Encoder.GetBytes(charBuffer, 0, 1, lowerCaseBytes, 0, false);
                            for (int j = 0; j < bytesCount; j++)
                                list.Add(lowerCaseBytes[j]);
                            skippedBytes.Clear();
                        }
                        else
                        {
                            while (skippedBytes.Count != 0)
                            {
                                var s = skippedBytes.Dequeue();
                                if (s == this._flag)
                                    list.Add(this._flag);
                                list.Add(s);
                            }
                            break;
                        }
                    }
                }
                yield return list.ToArray();
            }
            this.Decoder.Reset();
            this.Encoder.Reset();
        }

        public IAsyncEnumerable<byte> Decode(IAsyncEnumerable<byte> input)
        {
            throw new System.NotImplementedException();
        }
    }
}