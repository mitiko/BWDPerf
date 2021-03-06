using System.Collections.Generic;

namespace BWDPerf.Tools
{
    public class OccurenceDictionary<TKey> : Dictionary<TKey, int>
    {
        public void Add(TKey key)
        {
            base.TryGetValue(key, out var currentCount);
            base[key] = currentCount + 1;
        }

        public int Sum()
        {
            int sum = 0;
            foreach (var x in base.Values)
                sum += x;
            return sum;
        }
    }
}