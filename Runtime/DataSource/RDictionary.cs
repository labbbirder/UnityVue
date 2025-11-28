
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BBBirder.UnityVue
{

    public partial class RDictionary<TKey, TValue> : CollectionBase, IWatchableDic<TKey, TValue>
    {
        Dictionary<TKey, TValue> _dictionary = new();
        [field: NonSerialized] WatchablePayload IWatchable.Payload { get; } = new();
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        IWatchableCollection AsProxy => this;
        WatchablePayload Payload => AsProxy.Payload;
        private static object _k_count = new();
        public int Count
        {
            get
            {
                Payload.onBeforeGet?.Invoke(this, _k_count);
                return _dictionary.Count;
            }
        }
        public bool IsReadOnly => false;

        internal static Func<TKey, object> boxer;

        static RDictionary()
        {
            RuntimeHelpers.RunClassConstructor(typeof(RDictInitializer<TValue>).TypeHandle);
        }

        public TValue this[TKey key]
        {
            get
            {
                Payload.onBeforeGet?.Invoke(this, boxer != null ? boxer(key) : key);
                return _dictionary[key];
            }
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary[key] = value;
                    Payload.onAfterSet?.Invoke(this, boxer != null ? boxer(key) : key);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public override void RemoveByKey(object key)
        {
            Remove((TKey)key);
        }

        public override void ClearAll()
        {
            Clear();
        }

        public void Add(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key)) throw new ArgumentException($"key {key} already exists");
            using var _ = AsProxy.StartAddOperation(key, value);
            _dictionary.Add(key, value);
            Payload.onAfterSet?.Invoke(this, key);
            Payload.onAfterSet?.Invoke(this, _k_count);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        [ThreadStatic] static Stack<TKey> t_removingkeys;
        public void Clear()
        {
            using var _ = AsProxy.StartClearOperation();

            t_removingkeys ??= new();
            t_removingkeys.Clear();
            foreach (var (k, v) in _dictionary)
            {
                t_removingkeys.Push(k);
            }

            _dictionary.Clear();

            foreach (var k in t_removingkeys)
            {
                Payload.onAfterSet?.Invoke(this, k);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                t_removingkeys.Clear();
            }

            Payload.onAfterSet?.Invoke(this, _k_count);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var value)
                && EqualityComparer<TValue>.Default.Equals(value, item.Value)
                ;
        }

        public bool ContainsKey(TKey key)
        {
            var found = _dictionary.ContainsKey(key);
            Payload.onBeforeGet?.Invoke(this, key);
            return found;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            Payload.onBeforeGet?.Invoke(this, _k_count);
            return _dictionary.GetEnumerator();
        }
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public override object RawGet(object key)
        {
            if (key == _k_count)
                return _dictionary.Count.BoxNumber();

            if (_dictionary.TryGetValue((TKey)key, out var val))
                return val;

            return default(TValue);
        }

        public override bool RawSet(object key, object value)
        {
            // var found = _dictionary.ContainsKey((TKey)key);
            _dictionary[(TKey)key] = (TValue)value;
            return true;
        }

        private void RemoveInternal(TKey key, TValue v)
        {
            using var _ = AsProxy.StartRemoveOperation(key, v);
            var found = _dictionary.Remove(key);
            Payload.onAfterSet?.Invoke(this, key);
            Payload.onAfterSet?.Invoke(this, _k_count);
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out var v))
            {
                RemoveInternal(key, v);
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_dictionary.Contains(item))
            {
                RemoveInternal(item.Key, item.Value);
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var found = _dictionary.TryGetValue(key, out value);
            Payload.onBeforeGet?.Invoke(this, key);
            return found;
        }

        public TValue GetValueOrDefault(TKey key, TValue value = default)
        {
            if (TryGetValue(key, out var v))
            {
                return v;
            }

            return value;
        }

    }

    static class RDictInitializer<TValue>
    {
        static RDictInitializer()
        {
            RDictionary<int, TValue>.boxer = CastUtils.BoxNumber;
            RDictionary<uint, TValue>.boxer = CastUtils.BoxNumber;
            RDictionary<short, TValue>.boxer = CastUtils.BoxNumber;
            RDictionary<ushort, TValue>.boxer = CastUtils.BoxNumber;
            RDictionary<sbyte, TValue>.boxer = CastUtils.BoxNumber;
            RDictionary<byte, TValue>.boxer = CastUtils.BoxNumber;
        }
    }
}
