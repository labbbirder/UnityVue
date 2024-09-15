
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BBBirder.UnityVue
{
    public enum PreservedWatchableFlags : byte
    {
        Reactive = 128,
        Synchronize = 64,
    }

    public partial interface IWatchable : IComparable
    {
        // int SyncId { get; set; }
        byte StatusFlags { get; set; }
        // bool IsProxyInited { get; set; }
        Action<IWatchable, object> onPropertySet { get; set; }
        Action<IWatchable, object> onPropertyGet { get; set; }
        Dictionary<object, ScopeCollection> Scopes { get; set; }

        //  TODO: 如果是object类型，那么有可能漏掉代理对象
        /// <summary>
        /// Override me to improve performance
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsPropertyWatchable(object key)
        {
            return RawGet(key) is IWatchable;
        }

        object RawGet(object key);
        bool RawSet(object key, object value);

        // This feature is required by Odin Inspector
        int IComparable.CompareTo(object other)
        {
            return GetHashCode() - other.GetHashCode();
        }
    }

    public enum CollectionOperationType
    {
        None,
        Add,
        Remove,
        Clear,
    }
    public interface IWatchableCollection : IWatchable
    {
        CollectionOperationType operation { get; set; }
        Action<object, object> onAddItem { get; set; }
        Action<object, object> onRemoveItem { get; set; }
        Action onClearItems { get; set; }
        void RemoveByKey(object key);
        void ClearAll();
        AtomicCollectionOperation StartClearOperation()
        {
            if (onClearItems == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Clear, null, null);
        }
        AtomicCollectionOperation StartAddOperation(int index, object item)
        {
            if (onAddItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Add, index.BoxNumber(), item);
        }
        AtomicCollectionOperation StartRemoveOperation(int index, object item)
        {
            if (onRemoveItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Remove, index.BoxNumber(), item);
        }
        AtomicCollectionOperation StartAddOperation(object key, object item)
        {
            if (onAddItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Add, key, item);
        }
        AtomicCollectionOperation StartRemoveOperation(object key, object item)
        {
            if (onRemoveItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Remove, key, item);
        }

    }

    // public interface IWatchableCollection<T> : ICollection<T>, IWatchable
    // {
    //     CollectionOperationType operation { get; set; }
    //     Action<T> onAddItem { get; set; }
    //     Action<T> onRemoveItem { get; set; }
    //     void ASD(){onAddItem(new object());}
    // }

    public interface IWatchableList : IList, IWatchableCollection
    {
    }

    public interface IWatchableList<T> : IWatchableList, IList<T>
    {
    }

    public interface IWatchableDic<K, V> : IDictionary<K, V>, IWatchableCollection
    {
    }
}
