using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public abstract partial class CollectionBase : IWatchableCollection
    {
        [field: NonSerialized] CollectionOperationType IWatchableCollection.operation { get; set; }
        [field: NonSerialized] public Action<IWatchableCollection, object, object> onAddItem { get; set; }
        [field: NonSerialized] public Action<IWatchableCollection, object, object> onRemoveItem { get; set; }
        [field: NonSerialized] public Action<IWatchableCollection> onClearItems { get; set; }

        [field: NonSerialized] WatchablePayload IWatchable.Payload { get; } = new();

        public abstract object RawGet(object key);

        public abstract bool RawSet(object key, object value);

        public abstract void ClearAll();

        public abstract void RemoveByKey(object key);
    }
}
