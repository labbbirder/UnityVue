using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBBirder.UnityVue
{
    public static class ScopeLifeKeeperExtensions
    {
        [DebuggerHidden]
        public static WatchScope WatchEffect(this IScopeLifeKeeper lifeKeeper,
            Action effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.WatchEffect(effect, argument: argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            Func<T> wf, Action<T, T> effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            IValuedData<T> wf, Action<T, T> effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            Func<T> wf, Action<T> effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            IValuedData<T> wf, Action<T> effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            Func<T> wf, Action effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<T>(this IScopeLifeKeeper lifeKeeper,
            IValuedData<T> wf, Action effect, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var scp = CSReactive.Watch(wf, effect, argument);
            return scp;
        }

        [DebuggerHidden]
        public static Computed<TResult> Compute<TResult>(this IScopeLifeKeeper lifeKeeper,
            Func<TResult> expf, ScopeArgument argument = default)
        {
            argument.lifeKeeper = lifeKeeper;
            var computed = CSReactive.Compute(expf, out var scp, argument);
            return computed;
        }

        public static void ReleaseScopes(this IScopeLifeKeeper keeper)
        {
            var scopes = keeper.Scopes;
            foreach (var scope in scopes)
            {
                scope.Dispose();
            }

            scopes.Clear();
        }

        public static void UpdateDirtyScopes(this IScopeLifeKeeper keeper)
        {
            var scopes = keeper.Scopes;
            foreach (var scope in scopes)
            {
                // if (scope.isDirty)
                // {
                CSReactive.RunScope(scope, invokeNormalEffect: true, checkEnable: false);
                // }
            }
        }

        // [DebuggerHidden]
        // public static WatchScope WatchEffect<TComp>(this TComp component,
        //     Action effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {

        //     return component.GetLifeKeeper().WatchEffect(effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     Func<T> wf, Action<T, T> effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     IValuedData<T> wf, Action<T, T> effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     Func<T> wf, Action<T> effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     IValuedData<T> wf, Action<T> effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     Func<T> wf, Action effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static WatchScope Watch<TComp, T>(this TComp component,
        //     IValuedData<T> wf, Action effect, ScopeArgument argument = default)
        //     where TComp : Object
        // {
        //     return component.GetLifeKeeper().Watch(wf, effect, argument);
        // }

        // [DebuggerHidden]
        // public static Computed<T> Compute<T>(this Component component,
        //     Func<T> expf, ScopeArgument argument = default)
        // {
        //     return component.GetLifeKeeper().Compute(expf, argument);
        // }
    }
}
