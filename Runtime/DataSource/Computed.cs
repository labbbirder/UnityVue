using System;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public partial class Computed<T> : IValuedData<T>, IWatchable
    {
        internal T cached;
        WatchScope scope;
        Func<T> getter;
        [field: NonSerialized] public WatchablePayload Payload { get; } = new();

        public T Value
        {
            get
            {
                var topScope = CSReactive.s_executingScope;

                if (scope == null) return default;

                if (scope.isDirty)
                {
                    CSReactive.RunScope(scope);
                }

                if (topScope != null)
                {
                    foreach (var collection in scope.includedTables)
                    {
                        collection.Add(topScope);
                        topScope.includedTables.Add(collection);
                    }
                }

                return cached;
            }
        }

        [Obsolete("Use CSReactive.Computed instead")]
        internal Computed(Func<T> getter)
        {
            this.getter = getter;
        }

        public T GetValue() => Value;

        internal void SetScope(WatchScope scope)
        {
            scope.isDirty = true;
            scope.autoClearDirty = false;
            this.scope = scope;
        }

        public void Update()
        {
            cached = getter();
        }

        public static implicit operator T(Computed<T> computed) => computed is null ? default : computed.Value;

        // public static implicit operator Func<T>(Computed<T> computed) => () => computed.Value;

        object IWatchable.RawGet(object key) => null;
        bool IWatchable.RawSet(object key, object value) => false;
    }
}
