//author: bbbirder
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

namespace BBBirder.UnityVue
{
    [Serializable]
    [DebuggerTypeProxy(typeof(RList<>.RListDebugView))]
    public partial class RList<T> : CollectionBase, IWatchableList<T>, IList, IDisposable
    {
        // private const int DefaultCapacity = 4;
        static readonly T[] s_empty = Array.Empty<T>();
        static readonly EqualityComparer<T> DefaultComparer = EqualityComparer<T>.Default;
        static readonly bool IsElementWatchableType = typeof(IWatchable).IsAssignableFrom(typeof(T));
        static readonly ArrayPool<T> ArrayPool = ModuleProps<T>.ArrayPool;

        protected T[] _array = s_empty;
        int _Count = 0;

        IWatchableList<T> AsProxy => this;
        WatchablePayload Payload => AsProxy.Payload;

        public int Count
        {
            get
            {
                Payload.onBeforeGet?.Invoke(this, nameof(Count));
                return _Count;
            }
            protected set
            {
                if (_Count == value) return;
                _Count = value;
                Payload.onAfterSet?.Invoke(this, nameof(Count));
            }
        }

        public RList()
        {
            // If allocates with a default size, the array pool will returns an array with at least 16, which consumes too much memory.
            // EnsureSize(DefaultCapacity);
        }

        public RList(int capacity)
        {
            EnsureSize(capacity);
        }

        public override object RawGet(object key)
        {
            if (key is int index)
            {
                return GetByIndex(index);
            }

            if (key is string str && int.TryParse(str, out index))
            {
                return GetByIndex(index);
            }

            if (key is nameof(Count))
            {
                return _Count;
            }

            throw new ArgumentException($"key is not a number {key}");
        }

        public override bool RawSet(object key, object value)
        {
            if (key is int index)
            {
                _array[index] = (T)value;
                return true;
                // _array[index] = TypeUtils.CastTo<T>(value);
            }

            if (key is string str && int.TryParse(str, out index))
            {
                _array[index] = (T)value;
                return true;
            }

            throw new ArgumentException($"key is not a number {key}");
        }

        T GetByIndex(int index)
        {
            if (index < 0 || index >= _Count) return default;
            return _array[index];
        }

        /// <summary>
        /// access element and emit notification
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns> <summary>
        ///
        /// </summary>
        /// <value></value>
        public virtual T this[int index]
        {
            get
            {
                Payload.onBeforeGet?.Invoke(this, index.BoxNumber());
                return GetByIndex(index);
            }
            set
            {
                if (index < 0 || index >= _Count)
                {
                    throw new IndexOutOfRangeException($"index {index} out of range[0..{_Count}]");
                }

                var prev = _array[index];
                if (DefaultComparer.Equals(prev, value)) return;
                _array[index] = value;
                Payload.onAfterSet?.Invoke(this, index.BoxNumber());
            }
        }

        public virtual void EnsureSize(int size)
        {
            if (_array.Length >= size) return;
            if (size == 0)
            {
                ReleaseArray(ref _array);
                _array = s_empty;
            }
            else
            {
                size = CollectionUtility.LargerSizeInPowOf2(size);
                var newArr = ArrayPool.Rent(size);
                // var newArr = new T[size];
                if (_Count > 0)
                {
                    Array.Copy(_array, newArr, _Count);
                }

                ReleaseArray(ref _array);
                _array = newArr;
            }
        }

        private void ReleaseArray(ref T[] array)
        {
            if (array != null && !ReferenceEquals(array, s_empty))
            {
                ArrayPool.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }

            array = s_empty;
        }

        #region Array-like Implements
        object _syncRoot;

        public bool IsReadOnly => false;

        public bool IsFixedSize { get; set; } = false;

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

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        /// <summary>
        /// Clear all elements without one-by-one notifaction, except `Count`.
        /// </summary>
        public virtual void RawClear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _Count);
            }

            Count = 0;
        }

        /// <summary>
        /// Clear all elements and notify elements change and count change.
        /// </summary>
        public virtual void Clear()
        {
            if (_Count == 0)
                return;

            using var _ = StartClearOperation();
            var cnt = _Count;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, cnt);
                _Count = 0;
                for (int i = cnt - 1; i >= 0; i--)
                {
                    Payload.onAfterSet?.Invoke(this, i.BoxNumber());
                }
            }
            else
            {
                _Count = 0;
                for (int i = cnt - 1; i >= 0; i--)
                {
                    Payload.onAfterSet?.Invoke(this, i.BoxNumber());
                }
            }

            Payload.onAfterSet?.Invoke(this, nameof(Count));
        }

        /// <summary>
        /// Add an element and notify
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(T item)
        {
            using var _ = StartAddOperation(_Count, item);
            EnsureSize(_Count + 1);
            _array[_Count] = item;
            _Count++;
            Payload.onAfterSet?.Invoke(this, nameof(Count));
            Payload.onAfterSet?.Invoke(this, (_Count - 1).BoxNumber());
        }

        public RList(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public RList(T[] items)
        {
            AddRange(items);
        }

        private AtomicCollectionOperation StartClearOperation()
        {
            if (AsProxy.onClearItems == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Clear, null, null);
        }
        private AtomicCollectionOperation StartAddOperation(int index, object addedItem)
        {
            if (AsProxy.onAddItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Add, index.BoxNumber(), addedItem);
        }
        private AtomicCollectionOperation StartRemoveOperation(int index, object removedItem)
        {
            if (AsProxy.onRemoveItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Remove, index.BoxNumber(), removedItem);
        }

        public virtual void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                using var _ = StartAddOperation(_Count, item);
                Add(item);
            }
        }

        public void AddRange(T[] items)
        {
            AddRange(items, 0, items.Length);
        }

        public void AddRange(T[] items, int startIndex, int count)
        {
            // modify
            EnsureSize(_Count + count);
            Array.Copy(items, startIndex, _array, _Count, count);
            int istart = _Count;
            int iend = _Count += count;

            // notify
            Payload.onAfterSet?.Invoke(this, nameof(Count));
            for (int i = istart; i < iend; i++)
            {
                using var _ = StartAddOperation(i, _array[i]);
                Payload.onAfterSet?.Invoke(this, i.BoxNumber());
            }
        }

        public void Insert(int index, T item)
        {
            using var _ = StartAddOperation(index, item);

            // modify
            EnsureSize(_Count + 1);
            for (int i = _Count; i > index; i--)
            {
                _array[i] = _array[~-i];
            }

            _array[index] = item;
            _Count += 1;

            // notify
            Payload.onAfterSet?.Invoke(this, nameof(Count));
            for (int i = index; i < _Count; i++)
            {
                Payload.onAfterSet?.Invoke(this, i.BoxNumber());
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _Count) return;
            var item = _array[index];
            using var op = StartRemoveOperation(index, item);
            for (int i = index; i < _Count - 1; i++)
            {
                _array[i] = _array[-~i];
            }

            _array[_Count - 1] = default(T);
            _Count--;

            for (int i = index; i < _Count; i++)
            {
                Payload.onAfterSet?.Invoke(this, i.BoxNumber());
            }

            Payload.onAfterSet?.Invoke(this, nameof(Count));
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        // public T[] ToArray()
        // {
        //     return this.AsEnumerable().ToArray();
        // }

        /// <summary>
        /// Copy without element-get notification
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(T[] array, int index)
        {
            Array.Copy(_array, 0, array, index, _Count);
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != ~0;
        }

        public int IndexOf(T item)
        {
            if (Payload.onBeforeGet is null)
            {
                return Array.IndexOf(_array, item);
            }
            else
            {
                for (int i = 0; i < _Count; i++)
                {
                    Payload.onBeforeGet.Invoke(this, i.BoxNumber());
                    if (DefaultComparer.Equals(item, _array[i]))
                    {
                        return i;
                    }
                }

                return ~0;
            }
        }

        public void Reverse()
        {
            int halfCount = _Count / 2;
            for (int i = 0, end = _Count - 1; i < halfCount; i++, end--)
            {
                (this[i], this[end]) = (this[end], this[i]);
            }
        }

        static StringBuilder sbuilder;
        public override string ToString()
        {
            const int MAX_DISPLAY_COUNT = 32;
            int bufferWidth = Console.BufferWidth;
            if (bufferWidth == 0) bufferWidth = int.MaxValue;
            sbuilder ??= new();
            sbuilder.Clear();
            sbuilder.Append($"RList<{typeof(T).Name}>");
            sbuilder.AppendLine($"[{_Count}] {{");

            IEnumerable<object> elements;
            var ellipse = new object();
            if (_Count > MAX_DISPLAY_COUNT)
            {
                elements = _array
                    .Take(MAX_DISPLAY_COUNT)
                    .OfType<object>()
                    .Append(ellipse)
                    ;
            }
            else
            {
                elements = _array
                    .Take(_Count)
                    .OfType<object>()
                    ;
            }

            int lineWidth = 0;
            foreach (var ele in elements)
            {
                var sitem = ele switch
                {
                    null => "null",
                    _ when ele == ellipse => "...",
                    _ when ele is string s => "\"" + s + "\"",
                    _ => ele.ToString(),
                };
                if (lineWidth == 0)
                {
                    sbuilder.Append(' ', 4);
                    lineWidth = 4;
                }

                var space = -lineWidth & 3;
                var step = sitem.Length + 2 + space;
                if (lineWidth > 4 && lineWidth + step > bufferWidth)
                {
                    sbuilder.AppendLine();
                    sbuilder.Append(' ', 4);
                    lineWidth = 4;
                }

                step -= space;
                space = -lineWidth & 3;
                step += space;
                sbuilder.Append(' ', space);
                sbuilder.Append(sitem);
                sbuilder.Append(", ");
                lineWidth += step;
            }

            sbuilder.Append("\n}");
            return sbuilder.ToString();
        }

        public Enumerator GetEnumerator()
        {
            Payload.onBeforeGet?.Invoke(this, nameof(Count));
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.Add(object value)
        {
            Add((T)value);
            return _Count;
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            try
            {
                Insert(index, (T)value);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"type:{typeof(T)}  value:{value} value_type:{value?.GetType()}. this:{this}");
                UnityEngine.Debug.LogException(e);
            }
        }

        void IList.Remove(object value)
        {
            Remove((T)value);
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        /// <summary>
        /// Release and return buffers to shared pool immediately
        /// </summary>
        /// <remarks>
        /// Optional Disposal: GC works well without `Dispose()`. No leak will happen.
        /// </remarks>
        public void Dispose()
        {
            ReleaseArray(ref _array);
        }

        public override void ClearAll()
        {
            Clear();
        }

        public override void RemoveByKey(object key)
        {
            RemoveAt((int)key);
        }

        public struct Enumerator : IEnumerator<T>
        {
            readonly IList<T> _list;
            int _index;
            T _current;

            public T Current => _current;
            object IEnumerator.Current => _current;

            public Enumerator(IList<T> list)
            {
                _list = list;
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index < _list.Count)
                {
                    _current = _list[_index];
                    _index++;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }
        }
        #endregion


        internal class RListDebugView
        {
            RList<T> list;
            public RListDebugView(RList<T> list)
            {
                this.list = list;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Values => list.ToArray();
        }
    }
}
