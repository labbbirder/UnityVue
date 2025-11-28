using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.LowLevel;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BBBirder.UnityVue
{

    public static class UnityVueDriver
    {
        public static int UnityReferenceCheckPerFrames = 10;
        private static RefData<float> _editorTime = new();
        private static RefData<float> _time = new();
        private static RefData<float> _fixedTime = new();
        private static RefData<float> _unscaledTime = new();
        private static RefData<float> _fixedUnscaledTime = new();

        private static SimpleList<(object key, PollingLifeKeeper keeper)> pollingLifeKeepers = new();
        private static Dictionary<object, PollingLifeKeeper> lutPollingLifeKeepers = new();
        private static int _frameIndex;
        public static Action onUpdate = CSReactive.UpdateDirtyScopes;

        public static float time
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    return _time.Value;
                }
                else
                {
                    return _editorTime.Value;
                }
#else
                return _time.Value;
#endif
            }
        }

        public static float fixedTime
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    return _fixedTime.Value;
                }
                else
                {
                    return _editorTime.Value;
                }
#else
                return _fixedTime.Value;
#endif
            }
        }

        public static float unscaledTime
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    return _unscaledTime.Value;
                }
                else
                {
                    return _editorTime.Value;
                }
#else
                return _unscaledTime.Value;
#endif
            }
        }

        public static float fixedUnscaledTime
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    return _fixedUnscaledTime.Value;
                }
                else
                {
                    return _editorTime.Value;
                }
#else
                return _fixedUnscaledTime.Value;
#endif
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void Setup()
        {
            InitPlayerLoop();
            _editorTime.Value = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void SetupRuntime()
        {
            InitPlayerLoop();

            _time.Value = Time.time;
            _fixedTime.Value = Time.fixedTime;
            _unscaledTime.Value = Time.unscaledTime;
            _fixedUnscaledTime.Value = Time.fixedUnscaledTime;
        }

        static void InitPlayerLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var subSystems = playerLoop.subSystemList;
            var updateIndex = Array.FindIndex(subSystems, s => s.type == typeof(UnityEngine.PlayerLoop.Update));
            var fixedUpdateIndex = Array.FindIndex(subSystems, s => s.type == typeof(UnityEngine.PlayerLoop.FixedUpdate));

            var customIndex = Array.FindIndex(subSystems, s => s.type.Name == nameof(UnityVueDriver));
            if (customIndex == -1)
            {
                ref var updateLoop = ref subSystems[updateIndex];
                updateLoop.subSystemList = updateLoop.subSystemList.Prepend(new PlayerLoopSystem()
                {
                    type = typeof(UnityVueDriver),
                    updateDelegate = PreEffectUpdate,
                }).Append(new PlayerLoopSystem()
                {
                    type = typeof(UnityVueDriver),
                    updateDelegate = PostEffectUpdate,
                }).ToArray();

                ref var fixedUpdateLoop = ref subSystems[fixedUpdateIndex];
                updateLoop.subSystemList = updateLoop.subSystemList.Prepend(new PlayerLoopSystem()
                {
                    type = typeof(UnityVueDriver),
                    updateDelegate = PreEffectFixedUpdate,
                }).ToArray();

                PlayerLoop.SetPlayerLoop(playerLoop);
            }
        }

        public static bool TryGetPollingLifeKeeper(object key, out PollingLifeKeeper lifeKeeper)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null");
            }

            return lutPollingLifeKeepers.TryGetValue(key, out lifeKeeper);
        }

        public static void RegisterPollingLifeKeeper(object key, PollingLifeKeeper lifeKeeper)
        {
            if (key != null && lifeKeeper != null && lutPollingLifeKeepers.TryAdd(key, lifeKeeper))
            {
                pollingLifeKeepers.Add((key, lifeKeeper));
            }
        }

        internal static void CheckAndRemoveDestroyedUnityObjectReferences()
        {
            for (int i = pollingLifeKeepers.Count - 1; i >= 0; i--)
            {
                var (key, keeper) = pollingLifeKeepers[i];
                if (!keeper.IsAlive)
                {
                    keeper.Release();
                    pollingLifeKeepers.EmplacedRemoveAt(i);
                    lutPollingLifeKeepers.Remove(key);
                }
            }
        }

        static void EditorUpdate()
        {
            _editorTime.Value = (float)EditorApplication.timeSinceStartup;
        }

        static void PreEffectUpdate()
        {
            _time.Value = Time.time;
            _unscaledTime.Value = Time.unscaledTime;
        }

        static void PreEffectFixedUpdate()
        {
            _fixedTime.Value = Time.time;
            _fixedUnscaledTime.Value = Time.fixedUnscaledTime;
        }

        static void PostEffectUpdate()
        {
            if (++_frameIndex % UnityReferenceCheckPerFrames == 0)
            {
                CheckAndRemoveDestroyedUnityObjectReferences();
            }

            onUpdate?.Invoke();
        }
    }
}
