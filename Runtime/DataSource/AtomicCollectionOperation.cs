using System;
using UnityEngine.Assertions;

namespace BBBirder.UnityVue
{
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
                    collection.onAddItem?.Invoke(collection, key, item);
                    break;
                case CollectionOperationType.Remove:
                    collection.onRemoveItem?.Invoke(collection, key, item);
                    break;
                case CollectionOperationType.Clear:
                    collection.onClearItems?.Invoke(collection);
                    break;
            }
            collection.operation = prevOperation;
        }
    }
}
