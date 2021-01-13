using System.Collections.Generic;

namespace BWDPerf.Tools
{
    public class CountDictionary<TKey> : Dictionary<TKey, int>
    {
        public void Add(TKey key)
        {
            if (!base.ContainsKey(key))
                base.Add(key, 1);
            else
                base[key]++;
        }

        public new int this[TKey key]
        {
            get => base.ContainsKey(key) ? base[key] : 0;
            set => base[key] = value;
        }
    }
}