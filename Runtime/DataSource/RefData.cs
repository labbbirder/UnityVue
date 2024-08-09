using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;

namespace BBBirder.UnityVue
{
    public partial class RefData<T> : IWatchable
    {
        T __rawObject;
        public T Value
        {
            get
            {
                AsDataProxy.onPropertyGet?.Invoke(this, "Value");
                return __rawObject;
            }
            set
            {
                if (EqualityComparer<T>.Default.Equals(__rawObject, value)) return;
                __rawObject = value;
                AsDataProxy.onPropertySet?.Invoke(this, "Value");
            }
        }

        IWatchable AsDataProxy => this;
        byte IWatchable.StatusFlags { get; set; }

        Dictionary<object, ScopeCollection> IWatchable.Scopes { get; set; }
        Action<IWatchable, object> IWatchable.onPropertySet { get; set; }
        Action<IWatchable, object> IWatchable.onPropertyGet { get; set; }

        [Obsolete("It's not reasonable to instantiate RefData manually.", true)]
        public RefData() { }

        internal RefData(T t)
        {
            Init(t);
        }

        internal void Init(T t)
        {
            __rawObject = t;
        }

        object IWatchable.RawGet(object key)
        {
            if (key is not "Value")
            {
                return null;
            }
            return __rawObject;
        }

        bool IWatchable.RawSet(object key, object value)
        {
            if (key is not "Value")
            {
                return false;
            }
            __rawObject = (T)value;
            return true;
        }

        bool IWatchable.IsPropertyWatchable(object key) => typeof(IWatchable).IsAssignableFrom(typeof(T));

        public static implicit operator T(RefData<T> self)
        {
            return self.Value;
        }

        // public static implicit operator Expression<Func<object>>(RefData<T> self)
        // {
        //     return () => self.Value;
        // }
        // public static implicit operator Expression<Func<T>>(RefData<T> self)
        // {
        //     return () => self.Value;
        // }

    }
}
