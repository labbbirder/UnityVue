using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BBBirder.UnityVue
{
    public enum ScopeFlushMode
    {
        /// <summary>
        /// Update on the coming LateUpdate when data changed(default)
        /// </summary>
        /// <typeparam name="bool"></typeparam>
        LateUpdate,

        // PreUpdate, // It may be confusing for uses

        /// <summary>
        /// Update immediately when data changed
        /// </summary>
        /// <typeparam name="bool"></typeparam>
        Immediate,
    }

    public interface IScopeLifeKeeper
    {
        bool IsAlive { get; }
        event Action onDestroy;
    }

    public class WatchScope
    {
        internal const int DEFAULT_UPDATE_LIMIT = 100;

        public string name = "";
        public ScopeFlushMode flushMode;
        public IScopeLifeKeeper lifeKeeper;
        public int updateLimit = DEFAULT_UPDATE_LIMIT;
        public Action effect, normalEffect;

        internal bool isDirty = false;
        internal int updatedInOneFrame;
        internal int frameIndex;

        /// <summary>
        /// a reference copy from data account, remove self when clear dependencies
        /// </summary>
        /// <returns></returns>
        internal HashSet<((IWatchable, object), ScopeCollection)> includedTables = new();

        public WatchScope(Action effect) : this(effect, null) { }

        public WatchScope(Action effect, Action normalEffect)
        {
            this.effect = effect;
            this.normalEffect = normalEffect;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update -= CSReactive.EditorUpdateDirtyScopes;
                EditorApplication.update += CSReactive.EditorUpdateDirtyScopes;
            }
            else
#endif
            {
                if (!ReactiveManager.Instance)
                    CreateRuntimeManager();
            }
            // #if UNITY_EDITOR
            //                 com.bbbirder.unity.ScopeVisualizer.Instance.refs.Add(new(this));
            // #endif
            static void CreateRuntimeManager()
            {
                var manager = new GameObject(nameof(ReactiveManager));
                manager.AddComponent<ReactiveManager>();
                GameObject.DontDestroyOnLoad(manager);
                if (!ReactiveManager.Instance) throw new Exception("没有找到ScopeKeeper单例");
            }
        }

        public WatchScope WithArguments(WatchScopeArguments arguments)
        {
            if (!string.IsNullOrEmpty(arguments.name))
            {
                this.name = arguments.name;
            }

            if (arguments.updateLimit != 0)
            {
                this.updateLimit = arguments.updateLimit;
            }

            this.flushMode = arguments.flushMode;
            if (flushMode == ScopeFlushMode.Immediate)
            {
                if (isDirty)
                {
                    CSReactive.SetDirty(this, false);
                    CSReactive.RunScope(this);
                }
            }
            return this;
        }

        public WatchScope WithLifeKeeper(IScopeLifeKeeper lifeKeeper)
        {
            lifeKeeper.onDestroy += Dispose;
            this.lifeKeeper = lifeKeeper;
            return this;
        }

        public WatchScope WithLifeKeeper(UnityEngine.Object uo)
        {
            return WithLifeKeeper(new UnityReferenceLifeKeeper(uo));
        }

        public WatchScope WithLifeKeeper<T>(T uo) where T : UnityEngine.Object, IScopeLifeKeeper
        {
            return WithLifeKeeper(uo as IScopeLifeKeeper);
        }

        // /// <summary>
        // /// Bind lifecycle to a reference. Scope will get disposed automatically when target reference is garbage collected.
        // /// </summary>
        // /// <remarks>
        // /// Note that, by applying a reference, the scope won't be notified immediately when the reference is garbage collected.
        // /// The life check logic is only occurs on update.
        // /// </remarks>
        // public WatchScope WithRef<T>(T reference) where T : class
        // {
        //     if (reference is IScopeLifeKeeper lifeKeeper)
        //     {
        //         return WithLifeKeeper(lifeKeeper);
        //     }
        //     else
        //     {
        //         return WithLifeKeeper(ReactiveManager.Instance.GetReferenceLifeKeeper(reference));
        //     }
        // }

        /// <summary>
        /// Clean and unsubscribe this scope
        /// </summary>
        public void Dispose()
        {
            CSReactive.FreeScope(this);
        }
    }

    public struct WatchScopeArguments
    {
        /// <summary>
        /// The debug name.
        /// </summary>
        public string name;
        /// <summary>
        /// Determine when the scope got updated.
        /// </summary>
        public ScopeFlushMode flushMode;
        /// <summary>
        /// Determine the maximum times allowed to update in one frame.
        /// </summary>
        public int updateLimit;

        public static implicit operator WatchScopeArguments(string name) => new()
        {
            name = name,
        };

        public static implicit operator WatchScopeArguments(ScopeFlushMode flushMode) => new()
        {
            flushMode = flushMode,
        };

        public static implicit operator WatchScopeArguments(int updateLimit) => new()
        {
            updateLimit = updateLimit,
        };

        public static implicit operator WatchScopeArguments((string name, ScopeFlushMode flushMode) args) => new()
        {
            name = args.name,
            flushMode = args.flushMode,
        };

        public static implicit operator WatchScopeArguments((ScopeFlushMode flushMode, int updateLimit) args) => new()
        {
            flushMode = args.flushMode,
            updateLimit = args.updateLimit,
        };

        public static implicit operator WatchScopeArguments((string name, int updateLimit) args) => new()
        {
            name = args.name,
            updateLimit = args.updateLimit,
        };
    }
}
