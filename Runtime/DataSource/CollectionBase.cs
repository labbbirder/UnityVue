using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public abstract partial class CollectionBase : IWatchableCollection
    {
        CollectionOperationType IWatchableCollection.operation { get; set; }
        public Action<object, object> onAddItem { get; set; }
        public Action<object, object> onRemoveItem { get; set; }
        public Action onClearItems { get; set; }

        byte IWatchable.StatusFlags { get; set; }
        Action<IWatchable, object> IWatchable.onPropertySet { get; set; }
        Action<IWatchable, object> IWatchable.onPropertyGet { get; set; }
        Dictionary<object, ScopeCollection> IWatchable.Scopes { get; set; }

        public abstract object RawGet(object key);

        public abstract bool RawSet(object key, object value);

        public abstract void ClearAll();

        public abstract void RemoveByKey(object key);
    }
}
