using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public abstract class BindableBehaviour : ReactiveBehaviour, IDataBinder
    {
        public bool IsAlive => !!this;
        List<WatchScope> IDataBinder.m_AttributeScopes { get; set; }
        public IDataBinder AsDataBinder => this;
        public abstract bool IsBinded { get; }
        public event Action onDestroyed;

        protected override void OnInit()
        {
            if (IsBinded)
            {
                AsDataBinder.OnBindInternal();
            }

            CSReactive.Watch(() => IsBinded, isBinded =>
            {
                if (isBinded) AsDataBinder.OnBindInternal();
                else AsDataBinder.OnUnbindInternal();
            });
        }

        public virtual void OnBind() { }
        public virtual void OnUnbind() { }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AsDataBinder.OnUnbindInternal();
            onDestroyed?.Invoke();
            onDestroyed = null;
        }
    }
}
