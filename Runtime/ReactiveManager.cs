using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Assertions;

namespace BBBirder.UnityVue
{

    public class UnityReferenceLifeKeeper : IScopeLifeKeeper
    {
        public UnityEngine.Object uo;
        public event Action onDestroy;
        public bool IsAlive => uo;
        public UnityReferenceLifeKeeper(UnityEngine.Object refer)
        {
            uo = refer;
        }
        internal void DestroyImmediate()
        {
            onDestroy?.Invoke();
            onDestroy = null;
        }
    }

    [ExecuteAlways]
    public class ReactiveManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void Setup()
        {
            Assert.IsNotNull(Instance);
        }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void SetupRuntime()
        {
            _ = Instance;
            Assert.IsNotNull(Instance);
        }

        static ReactiveManager _instance;
        internal List<UnityReferenceLifeKeeper> unityObjectKeepers = new();
        public static ReactiveManager Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = Resources.FindObjectsOfTypeAll<ReactiveManager>().FirstOrDefault();
                }
                if (!_instance)
                {
                    var go = new GameObject(nameof(ReactiveManager), typeof(ReactiveManager))
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _instance = go.GetComponent<ReactiveManager>();
                }
                return _instance;
            }
        }
        public Action onUpdate = CSReactive.UpdateDirtyScopes;

        void Awake()
        {
            if (_instance && _instance != this)
            {
                if (Application.isPlaying)
                    Destroy(_instance);
                else
                    DestroyImmediate(_instance);
            }
            if (!_instance) _instance = this;
        }

        void CheckAndRemoveDestroyedUnityObjectReferences()
        {
            for (int i = unityObjectKeepers.Count - 1; i >= 0; i--)
            {
                var keeper = unityObjectKeepers[i];
                if (!keeper.IsAlive)
                {
                    keeper.DestroyImmediate();
                    unityObjectKeepers[i] = unityObjectKeepers[^1];
                    unityObjectKeepers.RemoveAt(unityObjectKeepers.Count - 1);
                }
            }
        }

        void LateUpdate()
        {
            CheckAndRemoveDestroyedUnityObjectReferences();
            onUpdate?.Invoke();
        }
    }
}
