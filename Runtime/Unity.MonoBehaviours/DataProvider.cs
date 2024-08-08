using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace BBBirder.UnityVue
{
    public abstract class DataProvider : MonoBehaviour
    {
        public IWatchable TypelessData { get; private set; }
        public List<DataBinder> listeners;
        protected virtual void Awake()
        {
            listeners = CollectionPool<List<DataBinder>, DataBinder>.Get();
        }
        protected virtual void OnDestroy()
        {
            listeners.Clear();
            CollectionPool<List<DataBinder>, DataBinder>.Release(listeners);
        }
        protected void SetData(IWatchable data)
        {
            TypelessData = data;
            foreach (var lis in listeners)
            {
                lis.Refresh();
            }
        }

        public abstract Type DataType { get; }
    }

    public abstract class DataProvider<T> : DataProvider where T : IWatchable
    {
#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public T Data
        {
            get => (T)TypelessData;
            set => SetData(value);
        }
        public override Type DataType => typeof(T);
        public static DataProvider<T> Instance
        {
            get;
            private set;
        }
        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }
        protected override void OnDestroy()
        {
            base.Awake();
            if (Instance == this) Instance = null;
        }
        public virtual void Refresh()
        {

        }
    }
}
