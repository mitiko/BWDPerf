using System;
using System.Collections.Generic;

namespace BWDPerf.Tools
{
    public class OccurenceDictionary<TKey> : Dictionary<TKey, int>
    {
        public OccurenceDictionary() { }
        public OccurenceDictionary(int capacity) : base(capacity) { }
        public OccurenceDictionary(OccurenceDictionary<TKey> dictionary) : base(dictionary) { }

        public void Add(TKey key)
        {
            base.TryGetValue(key, out var currentCount);
            base[key] = currentCount + 1;
        }

        public void Substract(TKey key)
        {
            base.TryGetValue(key, out var currentCount);
            if (currentCount <= 1)
                base.Remove(key);
            else
                base[key] = currentCount - 1;
        }

        public void SubstractMany(TKey key, int count)
        {
            base.TryGetValue(key, out var currentCount);
            if (currentCount <= count)
                base.Remove(key);
            else
                base[key] = currentCount - count;
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