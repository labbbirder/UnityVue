
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BBBirder.UnityVue
{
    public class WatchablePayload
    {
        public Action<IWatchable, object> onAfterSet;
        public Action<IWatchable, object> onBeforeGet;
        public readonly Dictionary<object, ScopeCollection> Scopes = new();

        public WatchablePayload()
        {
            onAfterSet = CSReactive.OnGlobalAfterSetProperty;
            onBeforeGet = CSReactive.OnGlobalBeforeGetProperty;
        }

        public void Clear()
        {
            onAfterSet = null;
            onBeforeGet = null;
            Scopes.Clear();
        }
    }

    public partial interface IWatchable
#if ODIN_INSPECTOR
        : IComparable
#endif
    {
        WatchablePayload Payload { get; }
        object RawGet(object key);
        bool RawSet(object key, object value);
#if ODIN_INSPECTOR
        // This feature is required by Odin Inspector
        int IComparable.CompareTo(object other)
        {
            return GetHashCode() - other.GetHashCode();
        }
#endif
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
#warning TODO: replace with DelegateList
        Action<IWatchableCollection, object, object> onAddItem { get; set; }
        Action<IWatchableCollection, object, object> onRemoveItem { get; set; }
        Action<IWatchableCollection> onClearItems { get; set; }
        void RemoveByKey(object key);
        void ClearAll();

        /// <summary>
        /// Dispatch a container clear event on leave scope
        /// </summary>
        /// <returns></returns>
        AtomicCollectionOperation StartClearOperation()
        {
            if (onClearItems == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Clear, null, null);
        }

        /// <summary>
        /// Dispatch an array-like container add event on leave scope
        /// </summary>
        /// <param name="index">The index of adding element</param>
        /// <param name="item">The value of adding element</param>
        /// <returns></returns>
        AtomicCollectionOperation StartAddOperation(int index, object item)
        {
            if (onAddItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Add, index.BoxNumber(), item);
        }

        /// <summary>
        /// Dipatch an array-like container remove event on leave scope
        /// </summary>
        /// <param name="index">The index of removing element</param>
        /// <param name="item">The value of removing element</param>
        /// <returns></returns>
        AtomicCollectionOperation StartRemoveOperation(int index, object item)
        {
            if (onRemoveItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Remove, index.BoxNumber(), item);
        }

        /// <summary>
        /// Dispatch a dict-like container add event on leave scope
        /// </summary>
        /// <param name="key">The key of adding element</param>
        /// <param name="item">The value of adding element</param>
        /// <returns></returns>
        AtomicCollectionOperation StartAddOperation(object key, object item)
        {
            if (onAddItem == null) return new();
            return new AtomicCollectionOperation(this, CollectionOperationType.Add, key, item);
        }

        /// <summary>
        /// Dispatch a dict-like container remove event on leave scope
        /// </summary>
        /// <param name="key">The key of removing element</param>
        /// <param name="item">The value of removing element</param>
        /// <returns></returns>
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

    public interface IWatchableDic<TKey, TValue> : IDictionary<TKey, TValue>, IWatchableCollection
    {
    }

    public interface IWatchableSet<T> : ISet<T>, IWatchableCollection
    {
    }
}
