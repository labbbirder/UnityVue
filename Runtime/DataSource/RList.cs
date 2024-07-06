//author: bbbirder
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Buffers;
using System.Text;

namespace BBBirder.UnityVue
{
    public class RList<T> : IWatchableList<T>, IList, IDisposable
    {
        static readonly EqualityComparer<T> DefaultComparer = EqualityComparer<T>.Default;
        static readonly bool IsElementWatchableType = typeof(IWatchable).IsAssignableFrom(typeof(T));
        static readonly ArrayPool<T> ArrayPool = CollectionUtility<T>.ArrayPool;

        bool IWatchable.IsProxyInited { get; set; }
        Action<object> IWatchable.onPropertySet { get; set; }
        Action<object> IWatchable.onPropertyGet { get; set; }
        bool IWatchable.IsPropertyWatchable(object key)
        {
            if (key is "Count")
            {
                return false;
            }
            return IsElementWatchableType;
        }
        IWatchable AsDataProxy => this;

        protected T[] _array = Array.Empty<T>();
        int _Count = 0;

        public int Count
        {
            get
            {
                AsDataProxy.onPropertyGet?.Invoke(nameof(Count));
                return _Count;
            }
            protected set
            {
                if (_Count == value) return;
                _Count = value;
                AsDataProxy.onPropertySet?.Invoke(nameof(Count));
            }
        }
        public RList(int capacity = 16)
        {
            EnsureSize(capacity);
        }
        object IWatchable.RawGet(object key)
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

        bool IWatchable.RawSet(object key, object value)
        {
            if (key is int index)
            {
                _array[index] = (T)value;
                // _array[index] = TypeUtils.CastTo<T>(value);
            }
            if (key is string str && int.TryParse(str, out index))
            {
                _array[index] = (T)value;
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
                AsDataProxy.onPropertyGet?.Invoke(index.BoxNumber());
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
                AsDataProxy.onPropertySet?.Invoke(index.BoxNumber());
            }
        }

        public virtual void EnsureSize(int size)
        {
            if (_array.Length >= size) return;
            size = CollectionUtility<T>.LargerSizeInPowOf2(size);
            var newArr = ArrayPool.Rent(size);
            if (_Count > 0)
            {
                Debug.Log($"{_array.Length} {size} {_Count}");
                Array.Copy(_array, newArr, _Count);
            }
            ArrayPool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _array = newArr;
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
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
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
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_array, 0, _Count);
            }
            for (int i = _Count - 1; i >= 0; i--)
            {
                AsDataProxy.onPropertySet?.Invoke(i.BoxNumber());
            }
            Count = 0;
        }

        /// <summary>
        /// Add an element and notify
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(T item)
        {
            EnsureSize(_Count + 1);
            _array[_Count] = item;
            _Count++;
            AsDataProxy.onPropertySet?.Invoke((_Count - 1).BoxNumber());
            AsDataProxy.onPropertySet?.Invoke(nameof(Count));
        }

        public RList(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public RList(T[] items)
        {
            AddRange(items);
        }

        public virtual void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void AddRange(T[] items)
        {
            AddRange(items, 0, items.Length);
        }

        public void AddRange(T[] items, int startIndex, int count)
        {
            EnsureSize(_Count + count);
            Array.Copy(items, startIndex, _array, _Count, count);

            int iend = _Count = _Count + count;
            for (int i = _Count; i < iend; i++)
            {
                AsDataProxy.onPropertySet?.Invoke(i.BoxNumber());
            }
            AsDataProxy.onPropertySet?.Invoke(nameof(Count));
        }

        public void Insert(int index, T item)
        {
            EnsureSize(_Count += 1);
            for (int i = ~-_Count; i > index; i--)
            {
                _array[i] = _array[~-i];
            }
            _array[index] = item;

            for (int i = index; i < _Count; i++)
            {
                AsDataProxy.onPropertySet?.Invoke(i.BoxNumber());
            }
            AsDataProxy.onPropertySet?.Invoke(nameof(Count));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _Count) return;
            for (int i = index; i < _Count - 1; i++)
            {
                this[i] = this[-~i];
            }
            this[_Count - 1] = default(T);
            Count--;
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

        public T[] ToArray()
        {
            return this.AsEnumerable().ToArray();
        }

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
            var idx = Array.IndexOf(_array, item);
            AsDataProxy.onPropertyGet?.Invoke(idx.BoxNumber());
            return ~0;
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

        public IEnumerator<T> GetEnumerator()
        {
            Debug.Log("Count " + Count);
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        public int Add(object value)
        {
            Add((T)value);
            return _Count;
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public void Remove(object value)
        {
            Remove((T)value);
        }

        public void CopyTo(Array array, int index)
        {
            CopyTo((T[])array, index);
        }

        public void Dispose()
        {
            ArrayPool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
        #endregion
    }
}
