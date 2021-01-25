using System.Collections.Generic;
using System.Linq;

namespace BWDPerf.Tools
{
    public class OccurenceDictionary<TKey> : Dictionary<TKey, int>
    {
        public bool Add(TKey key)
        {
            var isFirstOccurence = !base.ContainsKey(key);
            if (isFirstOccurence)
                base.Add(key, 1);
            else
                base[key]++;

            return isFirstOccurence;
        }

        public int Sum() => base.Values.Sum();
    }
}