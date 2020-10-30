using System.Collections.Generic;
using System.Linq;

namespace BWDPerf.Tools
{
    public class OccurenceDictionary<TKey> : Dictionary<TKey, int>
    {
        public void Add(TKey key)
        {
            if (!base.ContainsKey(key))
                base.Add(key, 1);
            else
                base[key]++;
        }

        public int Sum() => base.Values.Sum();
    }
}