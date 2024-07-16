
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace BBBirder.UnityVue
{
    internal static class CollectionUtility<T>
    {
        public static readonly ArrayPool<T> ArrayPool = ArrayPool<T>.Shared;
        public unsafe static int CeilExponent(int n)
        {
            float f = n;
            var exp = (int)(*(uint*)&f << 1 >> 24) - 127;
            // var exp = (*(int*)&f << 1 >>> 24) - 127; // C# 11.0
            if (1 << exp < n) exp++;
            return exp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LargerSizeInPowOf2(int cnt) => 1 << CeilExponent(cnt);

    }

    public struct AtomicCollectionOperation : IDisposable
    {
        bool hasValue;
        IWatchableCollection collection;
        CollectionOperationType prevOperation;
        object key, item;
        internal AtomicCollectionOperation(IWatchableCollection collection, CollectionOperationType operation, object key, object item)
        {
            Assert.IsNotNull(collection);
            hasValue = true;
            this.collection = collection;
            this.key = key;
            this.item = item;
            prevOperation = collection.operation;
            collection.operation = operation;
        }

        public void Dispose()
        {
            if (!hasValue) return;
            switch (collection.operation)
            {
                case CollectionOperationType.Add:
                    collection.onAddItem?.Invoke(key, item);
                    break;
                case CollectionOperationType.Remove:
                    collection.onRemoveItem?.Invoke(key, item);
                    break;
                case CollectionOperationType.Clear:
                    collection.onClearItems?.Invoke();
                    break;
            }
            collection.operation = prevOperation;
        }
    }
}
