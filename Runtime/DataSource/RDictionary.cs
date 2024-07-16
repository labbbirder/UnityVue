
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BBBirder.UnityVue
{
    public partial class RDictionary<TKey, TValue> : IWatchableDic<TKey, TValue>
    {
        Dictionary<TKey, TValue> _dictionary = new();
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        int IWatchable.SyncId { get; set; }
        byte IWatchable.StatusFlags { get; set; }
        Action<IWatchable, object> IWatchable.onPropertySet { get; set; }
        Action<IWatchable, object> IWatchable.onPropertyGet { get; set; }
        CollectionOperationType IWatchableCollection.operation { get; set; }
        Action<object, object> IWatchableCollection.onAddItem { get; set; }
        Action<object, object> IWatchableCollection.onRemoveItem { get; set; }
        Action IWatchableCollection.onClearItems { get; set; }

        private object _syncRoot;
        public bool IsSynchronized => false;
        public object SyncRoot
        {

            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new(), null);
                }
                return _syncRoot;
            }
        }

        IWatchableCollection AsProxy => this;
        private static object _k_count = new();
        public int Count
        {
            get
            {
                AsProxy.onPropertyGet?.Invoke(this, _k_count);
                return _dictionary.Count;
            }
        }
        public bool IsReadOnly => false;

        public TValue this[TKey key]
        {
            get
            {
                AsProxy.onPropertyGet?.Invoke(this, key);
                return _dictionary[key];
            }
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary[key] = value;
                    AsProxy.onPropertySet?.Invoke(this, key);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key)) throw new ArgumentException($"key {key} already exists");
            using var _ = AsProxy.StartAddOperation(key, value);
            _dictionary.Add(key, value);
            AsProxy.onPropertySet?.Invoke(this, key);
            AsProxy.onPropertySet?.Invoke(this, _k_count);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        static Stack<TKey> s_keys = new();
        public void Clear()
        {
            using var _ = AsProxy.StartClearOperation();

            s_keys ??= new();
            foreach (var (k, v) in _dictionary)
            {
                s_keys.Push(k);
            }

            foreach (var k in s_keys)
            {
                _dictionary[k] = default;
                AsProxy.onPropertySet?.Invoke(this, k);
            }

            _dictionary.Clear();
            AsProxy.onPropertySet?.Invoke(this, _k_count);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            var found = _dictionary.Contains(item);
            AsProxy.onPropertyGet?.Invoke(this, item.Key);
            return found;
        }

        public bool ContainsKey(TKey key)
        {
            var found = _dictionary.ContainsKey(key);
            AsProxy.onPropertyGet?.Invoke(this, key);
            return found;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public object RawGet(object key)
        {
            if (key == _k_count)
                return _dictionary.Count.BoxNumber();

            if (_dictionary.TryGetValue((TKey)key, out var val))
                return val;

            return default(TValue);
        }

        public bool RawSet(object key, object value)
        {
            var contains = _dictionary.ContainsKey((TKey)key);
            _dictionary[(TKey)key] = (TValue)value;
            return contains;
        }

        private void RemoveInternal(TKey key, TValue v)
        {
            using var _ = AsProxy.StartRemoveOperation(key, v);
            var found = _dictionary.Remove(key);
            AsProxy.onPropertySet?.Invoke(this, key);
            AsProxy.onPropertySet?.Invoke(this, _k_count);
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
            AsProxy.onPropertyGet?.Invoke(this, key);
            return found;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }

}
