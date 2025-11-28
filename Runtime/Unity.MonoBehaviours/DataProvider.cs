using System;
using System.Collections;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.Pool;

namespace BBBirder.UnityVue
{
    [Obsolete("Need Review")]
    public abstract class DataProvider : ReactiveBehaviour
    {
        [Reactive] public IWatchable TypelessData { get; set; }
        public abstract Type DataType { get; }
    }

    [Obsolete("Need Review")]
    public abstract class DataProvider<T> : DataProvider where T : IWatchable
    {
#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public T Data
        {
            get => (T)TypelessData;
            set => TypelessData = value;
        }
        public override Type DataType => typeof(T);

        public static DataProvider<T> Instance
        {
            get;
            private set;
        }

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
