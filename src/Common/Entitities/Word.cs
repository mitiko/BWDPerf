using System.Text;

namespace BWDPerf.Common.Entities
{
    public struct Word
    {
        public byte[] Content { get; set; }
        public int Size { get => this.Content.Length; }
        public int Count { get; set; }
        public bool IsPattern { get; }

        public Word(byte[] content, int count, bool isPattern = false)
        {
            this.Content = content;
            this.Count = count;
            this.IsPattern = isPattern;
        }

        public string ToString(Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            if (this.IsPattern) return "<s>";
            return encoding.GetString(this.Content);
        }
    }
}