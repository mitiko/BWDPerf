using System;
using System.Text;

namespace BWDPerf.Common.Entities
{
    public struct Word
    {
        public byte[] Content { get; set; }
        public int Size { get => this.Content.Length; }
        public int Count { get; set; }
        public float Rank { get; set; }
        
        public Word(byte[] content, int count)
        {
            this.Content = content;
            this.Count = count;
            this.Rank = 0; 
        }

        public string ToString(Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            return encoding.GetString(this.Content);
        }
    }
}