using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public abstract partial class ReactiveBehaviour : MonoBehaviour, IWatchable, IDataBinder
    {
        [field: NonSerialized] WatchablePayload IWatchable.Payload { get; } = new();
        public RefData<bool> _isEnabled = new(false);

        [field: NonSerialized] SimpleList<WatchScope> IScopeLifeKeeper.Scopes { get; } = new();
        [field: NonSerialized] public bool IsBinded { get; set; }

        public virtual bool IsEnabled => InternalEnabledState;

        protected internal bool InternalEnabledState
        {
            get => _isEnabled;
            private set => _isEnabled.Value = value;
        }

        bool IScopeLifeKeeper.IsAlive => !!this;
        public IDataBinder AsDataBinder => this;

        protected virtual void OnEnable()
        {
            InternalEnabledState = true;
        }

        protected virtual void OnDisable()
        {
            InternalEnabledState = false;
        }

        public object RawGet(object key)
        {
            ReactiveAttribute.TryGetInternal(this, key, out var value);
            return value;
        }

        public bool RawSet(object key, object value)
        {
            return ReactiveAttribute.TrySetInternal(this, key, value);
        }

        /// <summary>
        /// Init `ReactiveBehaviour`, in which this will be watch, meanwhile, property IsBind will be watch.
        /// Once Isbind is set to TRUE, all watching members will be bind immediately.
        /// </summary>
        protected virtual void Awake()
        {
            AsDataBinder.Bind();
        }

        protected virtual void OnDestroy()
        {
            AsDataBinder.Unbind();
            ; (this as IWatchable).Payload.Clear();
        }

        public virtual void OnBind() { }
        public virtual void OnUnbind() { }
    }
}
