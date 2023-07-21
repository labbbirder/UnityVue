using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using com.bbbirder;

namespace com.bbbirder
{

    internal class WeakKeyTable<V> : IDictionary<object, V>
    {
        Dictionary<int, List<(WeakReference, V)>> inner;

        object inner_locker;

        // Balance between cpu usage and memory allocation.
        readonly int BucketSize;
        public ICollection<object> Keys => throw new System.NotImplementedException();

        public ICollection<V> Values => throw new System.NotImplementedException();

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        void FastDrop(List<(WeakReference, V)> kvps, int i)
        {
            kvps[i] = kvps[~-kvps.Count];
            kvps.RemoveAt(~-kvps.Count);
            Count--;
        }

        public V this[object key]
        {
            get
            {
                var hash = GetHashCode(key);
                lock (inner_locker)
                {
                    if (inner.TryGetValue(hash, out var kvps))
                    {
                        for (int i = 0; i < kvps.Count; i++)
                        {
                            var (k, v) = kvps[i];
                            if (k.Target == key) return v;
                            if (k.Target is null) FastDrop(kvps, i);
                        }
                    }
                }
                return default;
            }
            set
            {
                var hash = GetHashCode(key);
                lock (inner_locker)
                {
                    if (!inner.TryGetValue(hash, out var kvps))
                    {
                        kvps = inner[hash] = new();
                    }
                    for (int i = 0; i < kvps.Count; i++)
                    {
                        var (k, v) = kvps[i];
                        if (k.Target == key)
                        {
                            if (value is null)
                                FastDrop(kvps, i);
                            else
                                kvps[i] = (k, value);
                            return;
                        }
                        if (k.Target is null)
                            FastDrop(kvps, i);
                    }
                    kvps.Add((new(key), value));
                }
                Count++;
            }
        }

        int GetHashCode(object k)
            => k.GetHashCode() % BucketSize;

        public WeakKeyTable(int bucketSize = 1024)
        {
            BucketSize = bucketSize;
            inner_locker = new();
            inner = new();
        }

        /// <summary>
        /// Sweep out all values whos key collected
        /// </summary>
        public void CleanGarbage()
        {
            lock (inner_locker)
            {

                foreach (var (hash, kvps) in inner)
                    for (int i = 0; i < kvps.Count; i++)
                    {
                        var (k, v) = kvps[i];
                        if (k.Target is null) FastDrop(kvps, i);
                    }
            }
        }

        public void Add(object key, V value)
        {
            this[key] = value;
        }

        public void Add(KeyValuePair<object, V> item)
        {
            this[item.Key] = item.Value;
        }

        public void Clear()
        {
            inner.Clear();
            Count = 0;
        }

        public bool Contains(KeyValuePair<object, V> item)
        {
            return EqualityComparer<V>.Default.Equals(this[item.Key], item.Value);
        }

        public bool ContainsKey(object key)
        {
            return this[key] != null;
        }

        public void CopyTo(KeyValuePair<object, V>[] array, int arrayIndex)
        {
            var idx = 0;
            var ibegin = arrayIndex;
            var iend = arrayIndex + array.Length;
            foreach (var e in this)
            {
                if (idx >= ibegin)
                    array[idx - ibegin] = e;
                if (idx >= iend)
                    break;
                idx++;
            }
        }

        public IEnumerator<KeyValuePair<object, V>> GetEnumerator()
        {
            foreach (var (hash, kvps) in inner)
                foreach (var kvp in kvps)
                    if (kvp.Item1.Target != null)
                        yield return new(kvp.Item1, kvp.Item2);
        }

        public bool Remove(object key)
        {
            var exists = ContainsKey(key);

            if (exists)
                this[key] = default;

            return exists;
        }

        public bool Remove(KeyValuePair<object, V> item)
        {
            var exists = Contains(item);

            if (exists)
                this[item.Key] = default;

            return exists;
        }

        public bool TryGetValue(object key, out V value)
        {
            value = this[key];
            return value != null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}