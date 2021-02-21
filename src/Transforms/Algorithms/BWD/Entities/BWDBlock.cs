namespace BWDPerf.Transforms.Algorithms.BWD.Entities
{
    public class BWDBlock
    {
        public BWDictionary Dictionary { get; }
        public BWDStream Stream { get; }

        public BWDBlock(BWDictionary dictionary, BWDStream stream)
        {
            this.Dictionary = dictionary;
            this.Stream = stream;
        }
    }
}