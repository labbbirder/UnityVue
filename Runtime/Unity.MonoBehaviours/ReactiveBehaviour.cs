using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public abstract class ReactiveBehaviour : MonoBehaviour, IWatchable
    {
        private bool _isInited;
        public int SyncId { get; set; }
        public byte StatusFlags { get; set; }
        public Action<IWatchable, object> onPropertySet { get; set; }
        public Action<IWatchable, object> onPropertyGet { get; set; }
        public Dictionary<object, ScopeCollection> Scopes { get; set; }

        public void Init()
        {
            if (_isInited) return;
            CSReactive.Reactive(this);
            OnInit();
            _isInited = true;
        }

        protected virtual void OnInit()
        {

        }

        public object RawGet(object key)
        {
            if (ReactiveAttribute.globalRawGetters[this.GetType()].TryGetValue(key, out var rawGetter))
            {
                return rawGetter(this);
            }
            return default;
        }

        public bool RawSet(object key, object value)
        {
            if (ReactiveAttribute.globalRawSetters[this.GetType()].TryGetValue(key, out var rawSetter))
            {
                rawSetter(this, value);
                return true;
            }
            return false;
        }

        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void OnDestroy()
        {
            Scopes?.Clear();
        }

    }
}
