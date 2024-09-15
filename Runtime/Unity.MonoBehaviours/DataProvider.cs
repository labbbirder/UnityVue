using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace BBBirder.UnityVue
{
    public abstract class DataProvider : ReactiveBehaviour
    {
        [Reactive] public IWatchable TypelessData { get; set; }
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
            set => TypelessData = value;
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
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }
    }
}
