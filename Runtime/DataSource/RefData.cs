using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    [Serializable]
    public partial class RefData<T> : IValuedData<T>, IWatchable
    {
        // static bool s_allowImplicitConversion;

        // static RefData()
        // {
        //     s_allowImplicitConversion = true
        //         && typeof(T) != typeof(RefData<T>)
        //         && typeof(T) != typeof(object)
        //         ;
        // }

        [UnityEngine.SerializeField] T __rawObject;

        [field: NonSerialized] WatchablePayload IWatchable.Payload { get; } = new();

        IWatchable AsDataProxy => this;

        public T Value
        {
            get
            {
                AsDataProxy.Payload.onBeforeGet?.Invoke(this, "Value");
                return __rawObject;
            }
            set
            {
                if (EqualityComparer<T>.Default.Equals(__rawObject, value)) return;
                __rawObject = value;
                AsDataProxy.Payload.onAfterSet?.Invoke(this, "Value");
            }
        }

        public RefData()
        {
            // mostly, deserialized by Unity
            // CSReactive.MakeProxy(this);
        }

        public RefData(T t)
        {
            __rawObject = t;
        }

        public T GetValue() => Value;

        bool RawSet<TValue>(object key, TValue value)
        {
            if (key is not "Value")
            {
                return false;
            }

            __rawObject = RuntimeConverter.Convert<TValue, T>(value);
            return true;
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

        public override string ToString()
        {
            return $"RefData<{TypeInfo<T>.Info.shortName}>[{Value?.ToString()}]";
        }

        public static implicit operator T(RefData<T> self)
        {
            return self.Value;
        }

        // public static implicit operator RefData<T>(T value)
        // {
        //     if (s_allowImplicitConversion)
        //     {
        //         return CSReactive.Ref(value);
        //     }
        //     else
        //     {
        //         throw new($"RefData<{TypeInfo<T>.Info.shortName}> is not allowed to convert implicitly, use `CSReactive.Ref` instead.");
        //     }
        // }

    }
}
