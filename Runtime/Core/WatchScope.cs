using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
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
        event Action onDestroyed;
    }

    public class WatchScope
    {
        internal const int DEFAULT_UPDATE_LIMIT = 100;

        public ScopeFlushMode flushMode;
        public IScopeLifeKeeper lifeKeeper;
        public int updateLimit = DEFAULT_UPDATE_LIMIT;
        public Action effect, normalEffect, onDisposed;
        internal bool hideInTracker = false;
        internal bool isDirty = false;
        internal int updatedInOneFrame;
        internal int frameIndex;
#if ENABLE_UNITY_VUE_TRACKER
        const int MAX_STACK_COUNT = 12;
        internal StackFrame[] stackFrames;
        internal string debugName;
#endif
        /// <summary>
        /// a reference copy from data account, remove self when clear dependencies
        /// </summary>
        /// <returns></returns>
        internal HashSet<ScopeCollection> includedTables = new();

        public WatchScope(Action effect) : this(effect, null, 2) { }

        public WatchScope(Action effect, Action normalEffect, int depth = 1)
        {
            this.effect = effect;
            this.normalEffect = normalEffect;
#if ENABLE_UNITY_VUE_TRACKER
            GlobalTrackerData.Instance.scopes.Add(new(this));
            stackFrames = new StackTrace(depth, true).GetFrames()
                .Take(MAX_STACK_COUNT)
                .ToArray();
#endif
        }

        public void SetFlushMode(ScopeFlushMode flushMode)
        {
            this.flushMode = flushMode;
            if (flushMode == ScopeFlushMode.Immediate)
            {
                if (isDirty)
                {
                    CSReactive.SetDirty(this, false);
                    CSReactive.RunScope(this);
                }
            }
        }

        public WatchScope WithArguments(WatchScopeArguments arguments)
        {
            if (arguments.updateLimit != 0)
            {
                this.updateLimit = arguments.updateLimit;
            }

            this.hideInTracker = arguments.hideInTracker;
            SetFlushMode(arguments.flushMode);
            return this;
        }

        public WatchScope WithLifeKeeper(IScopeLifeKeeper lifeKeeper)
        {
            lifeKeeper.onDestroyed -= Dispose;
            lifeKeeper.onDestroyed += Dispose;
            this.lifeKeeper = lifeKeeper;
            return this;
        }

        public WatchScope WithLifeKeeper(UnityEngine.Object uo)
        {
            var ur = new UnityReferenceLifeKeeper(uo);
            ReactiveManager.Instance.unityObjectKeepers.Add(ur);
            return WithLifeKeeper(ur);
        }

        public WatchScope WithLifeKeeper<T>(T uo) where T : UnityEngine.Object, IScopeLifeKeeper
        {
            return WithLifeKeeper(uo as IScopeLifeKeeper);
        }

        /// <summary>
        /// Execute once
        /// </summary>
        public void Update()
        {
            CSReactive.RunScope(this, true);
        }

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
        /// Determine when the scope got updated.
        /// </summary>
        public ScopeFlushMode flushMode;
        /// <summary>
        /// Determine the maximum times allowed to update in one frame.
        /// </summary>
        public int updateLimit;
        public bool hideInTracker;

        public static implicit operator WatchScopeArguments(ScopeFlushMode flushMode) => new()
        {
            flushMode = flushMode,
        };

        public static implicit operator WatchScopeArguments(int updateLimit) => new()
        {
            updateLimit = updateLimit,
        };

        public static implicit operator WatchScopeArguments((ScopeFlushMode flushMode, int updateLimit) args) => new()
        {
            flushMode = args.flushMode,
            updateLimit = args.updateLimit,
        };
    }
}
