using System;
using System.Collections;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public partial class EventTriggerContainer : CollectionBase
    {
        Dictionary<Action<object>, Action<object, object>> lut = new();
        public override void ClearAll()
        {
        }

        public void Emit(object evt)
        {
            (this as IWatchableCollection).onAddItem?.Invoke(0.BoxNumber(), evt);
        }

        public void AddListener(Action<object> listener)
        {
            if (!lut.TryGetValue(listener, out var realListener))
            {
                lut.Add(listener, realListener = (k, v) => listener(v));
            }
            (this as IWatchableCollection).onAddItem += realListener;
        }

        public void RemoveListener(Action<object> listener)
        {
            if (lut.TryGetValue(listener, out var realListener))
            {
                (this as IWatchableCollection).onAddItem -= realListener;
            }
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
