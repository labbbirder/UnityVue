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
        /// Update on the next `PostUpdate`(after Update and before LateUpdate) when data changed(default)
        /// </summary>
        PostUpdate,

        /// <summary>
        /// Update immediately when data changed
        /// </summary>
        Immediate,
    }

    public class WatchScope
    {
        private bool isDisposed;
        internal ScopeFlushMode flushMode;
        internal IScopeLifeKeeper lifeKeeper;
        private bool ignoreEnableState;

        public Action checker, normalEffect, rawNormalEffect, onDisposed;
        internal bool hideInTracker = false;
        internal RefData<bool> isEnabledSelf = new(true);
        internal bool isDirty = false;
        internal bool autoClearDirty = true;
        internal bool IsEnabled => ignoreEnableState ? true : isEnabledSelf && (lifeKeeper?.IsEnabled ?? true);
        public ScopeFlushMode FlushMode => flushMode;
        public IScopeLifeKeeper LifeKeeper => lifeKeeper;
#if ENABLE_UNITY_VUE_TRACKER
        const int MAX_STACK_COUNT = 12;
        internal StackFrame[] stackFrames;
#endif
        private string debugName;
        /// <summary>
        /// a reference copy from data account, remove self when clear dependencies
        /// </summary>
        /// <returns></returns>
        internal HashSet<ScopeCollection> includedTables = new();
        public bool IsDisposed => isDisposed;

        internal WatchScope(Action effect, ScopeArgument argument = default) : this(effect, null, argument, 2) { }

        internal WatchScope(Action effect, Action normalEffect, ScopeArgument argument, int depth = 1)
        {
            this.isDisposed = false;
            this.checker = effect;
            this.normalEffect = normalEffect;
#if ENABLE_UNITY_VUE_TRACKER
            GlobalTrackerData.Instance.scopes.Add(new(this));
            stackFrames = new StackTrace(depth, true).GetFrames()
                .Take(MAX_STACK_COUNT)
                .ToArray();
#endif
            this.flushMode = argument.flushMode;
            this.ignoreEnableState = argument.ignoreEnableState;
            this.hideInTracker = argument.hideInTracker;

            if (argument.lifeKeeper is { } lifeKeeper)
            {
                lifeKeeper.Scopes.Add(this);
                this.lifeKeeper = lifeKeeper;
            }

            if (this.flushMode is ScopeFlushMode.Immediate)
            {
                if (isDirty)
                {
                    CSReactive.SetDirty(this, false);
                    CSReactive.RunScope(this);
                }
            }
        }

        /// <summary>
        /// Execute once
        /// </summary>
        public WatchScope Update()
        {
            // CSReactive.RunScope(this, true);
            // The followings may be more reasonable ...
            rawNormalEffect?.Invoke();
            return this;
        }

        [Conditional("DEBUG")]
        public void SetDebugName(string name)
        {
            debugName = name;
        }
        public override string ToString()
        {
            return $"WatchScope({debugName})";
        }

        /// <summary>
        /// Clean and unsubscribe this scope
        /// </summary>
        public void Dispose()
        {
            // TODO: when a lifekeeper dies, `removeCallbackInLifeKeeper` should be passed with false
            CSReactive.FreeScope(this, removeCallbackInLifeKeeper: true);
            isDisposed = true;
        }
    }

    public struct ScopeArgument
    {
        /// <summary>
        /// Determine when the scope got updated.
        /// </summary>
        public ScopeFlushMode flushMode;
        public bool hideInTracker;
        public IScopeLifeKeeper lifeKeeper;
        public bool ignoreEnableState;

        public static implicit operator ScopeArgument(ScopeFlushMode flushMode) => new()
        {
            flushMode = flushMode,
        };

        public static implicit operator ScopeArgument((ScopeFlushMode flushMode, int updateLimit) args) => new()
        {
            flushMode = args.flushMode,
        };
    }
}
