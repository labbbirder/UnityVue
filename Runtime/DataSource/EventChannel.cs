using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    /// <summary>
    /// A pure data helps to do event-like sending and receiving.
    /// </summary>
    public partial class EventChannel<T> : CollectionBase
    {
        [NonSerialized] private readonly object _dummy = 0.BoxNumber();
        [NonSerialized] Dictionary<Action<T>, Action<IWatchableCollection, object, object>> lut = new();
        public override void ClearAll()
        {
        }

        public void Emit(T evt)
        {
            ; (this as IWatchableCollection).onAddItem?.Invoke(this, _dummy, evt);
        }

        public void AddListener(Action<T> listener)
        {
            if (!lut.TryGetValue(listener, out var realListener))
            {
                lut.Add(listener, realListener = (collection, _, evt) => listener((T)evt));
            }

            ; (this as IWatchableCollection).onAddItem += realListener;
        }

        public void RemoveListener(Action<T> listener)
        {
            if (lut.TryGetValue(listener, out var realListener))
            {
                ; (this as IWatchableCollection).onAddItem -= realListener;
                lut.Remove(listener);
            }
        }

        public void ClearAllListeners()
        {
            lut.Clear();
            ; (this as IWatchableCollection).onAddItem = null;
        }

        public override object RawGet(object key)
        {
            return null;
        }

        public override bool RawSet(object key, object value)
        {
            return false;
        }

        public override void RemoveByKey(object key)
        {
        }
    }
}
