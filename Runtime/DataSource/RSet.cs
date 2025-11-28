
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BBBirder.UnityVue
{
    public partial class RSet<TValue> : CollectionBase, IWatchableCollection, IEnumerable
    {
        private static object _k_count = new();

        HashSet<TValue> _set = new();
        [field: NonSerialized] WatchablePayload IWatchable.Payload { get; } = new();

        IWatchableCollection AsProxy => this;
        WatchablePayload Payload => AsProxy.Payload;

        public int Count
        {
            get
            {
                Payload.onBeforeGet?.Invoke(this, _k_count);
                return _set.Count;
            }
        }

        public override void RemoveByKey(object key)
        {
            Remove((TValue)key);
        }

        public override void ClearAll()
        {
            Clear();
        }

        public void Add(TValue value)
        {
            if (_set.Add(value))
            {
                using var _ = AsProxy.StartAddOperation(value, CastUtils.True);
                Payload.onAfterSet?.Invoke(this, value);
                Payload.onAfterSet?.Invoke(this, _k_count);
            }
        }

        [ThreadStatic] static Stack<TValue> t_removingValues;
        public void Clear()
        {
            using var _ = AsProxy.StartClearOperation();

            t_removingValues ??= new();
            t_removingValues.Clear();
            foreach (var v in _set)
            {
                t_removingValues.Push(v);
            }

            _set.Clear();

            foreach (var v in t_removingValues)
            {
                Payload.onAfterSet?.Invoke(this, v);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                t_removingValues.Clear();
            }

            Payload.onAfterSet?.Invoke(this, _k_count);
        }

        public bool Contains(TValue value)
        {
            var found = _set.Contains(value);
            Payload.onBeforeGet?.Invoke(this, value);
            return found;
        }

        public Enumerator GetEnumerator()
        {
            Payload.onBeforeGet?.Invoke(this, _k_count);
            return new(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override object RawGet(object key)
        {
            if (key == _k_count)
                return _set.Count.BoxNumber();

            if (_set.Contains((TValue)key))
                return CastUtils.True;

            return CastUtils.False;
        }

        public override bool RawSet(object key, object value)
        {
            if ((bool)value)
            {
                _set.Add((TValue)key);
            }
            else
            {
                _set.Remove((TValue)key);
            }

            return true;
        }

        public bool Remove(TValue value)
        {
            if (_set.Remove(value))
            {
                using var _ = AsProxy.StartRemoveOperation(value, CastUtils.True);
                Payload.onAfterSet?.Invoke(this, value);
                Payload.onAfterSet?.Invoke(this, _k_count);
                return true;
            }

            return false;
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            readonly RSet<TValue> _set;
            HashSet<TValue>.Enumerator _enumerator;

            public TValue Current
            {
                get
                {
                    var current = _enumerator.Current;
                    _set.Payload.onBeforeGet?.Invoke(_set, current);
                    return current;
                }
            }

            object IEnumerator.Current => Current;

            internal Enumerator(RSet<TValue> set)
            {
                _set = set;
                _enumerator = _set._set.GetEnumerator();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator = _set._set.GetEnumerator();
            }
        }

    }
}
