
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BBBirder.UnityVue
{
    public interface IWatchable : IComparable
    {
        bool IsProxyInited { get; set; }
        Action<object> onPropertySet { get; set; }
        Action<object> onPropertyGet { get; set; }

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

    public interface IWatchableList : IList, IWatchable { }
    public interface IWatchableList<T> : IList<T>, IWatchable { }
    public interface IWatchableDic<K, V> : IDictionary<K, V>, IWatchable { }
}
