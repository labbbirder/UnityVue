using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace BBBirder.UnityVue
{
    public abstract partial class CSReactive
    {
        /** UPDATE: watchable will be watched by default, so we dont need it anymore. **/
        // /// <summary>
        // /// Make a watchable object watched
        // /// </summary>
        // /// <typeparam name="T"></typeparam>
        // /// <param name="watchable"></param>
        // /// <returns></returns>
        // public static T Reactive<T>(T watchable) where T : IWatchable
        // {
        //     if (watchable is null)
        //         return watchable;

        //     return MakeProxy(watchable);
        // }

        /// <summary>
        /// Create a RefData from a plain object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static RefData<T> Ref<T>(T data)
        {
            // var refData = ObjectPool<RefData<T>>.Get();
            // refData.Init(data);
            return new RefData<T>(data);
        }

        public static WatchScope WatchEffect(Action effect, ScopeArgument argument = default)
        {
            AssertMainThread();
            var scp = new WatchScope(effect, argument: argument);
            RunScope(scp);
            return scp;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wf"></param>
        /// <param name="effect">parameters receive `currentValue`, `previousValue` in order</param>
        /// <returns></returns>
        public static WatchScope Watch<T>(Func<T> wf, Action<T, T> effect, ScopeArgument argument = default)
        {
            AssertMainThread();
            T prev = default, curr = default;
            var scp = new WatchScope(RunCheck, RunEffect, argument)
            {
                rawNormalEffect = RunRawEffect
            };

            RunScope(scp, false);
            return scp;

            [DebuggerHidden]
            void RunRawEffect()
            {
                effect.Invoke(curr, curr);
            }

            [DebuggerHidden]
            void RunCheck()
            {
                prev = curr;
                curr = wf();
            }

            [DebuggerHidden]
            void RunEffect()
            {
                if (!EqualityComparer<T>.Default.Equals(curr, prev))
                {
                    effect.Invoke(curr, prev);
                }
            }
        }

        public static WatchScope Watch<T>(Func<T> wf, Action<T> effect, ScopeArgument argument = default)
        {
            AssertMainThread();
            T prev = default, curr = default;
            var scp = new WatchScope(RunCheck, RunEffect, argument)
            {
                rawNormalEffect = RunRawEffect
            };

            RunScope(scp, false);
            return scp;

            [DebuggerHidden]
            void RunRawEffect()
            {
                effect.Invoke(curr);
            }

            [DebuggerHidden]
            void RunCheck()
            {
                prev = curr;
                curr = wf();
            }

            [DebuggerHidden]
            void RunEffect()
            {
                if (!EqualityComparer<T>.Default.Equals(curr, prev))
                {
                    effect.Invoke(curr);
                }
            }
        }

        public static WatchScope Watch<T>(Func<T> wf, Action effect, ScopeArgument argument = default)
        {
            AssertMainThread();
            T prev = default, curr = default;
            var scp = new WatchScope(RunCheck, RunEffect, argument)
            {
                rawNormalEffect = effect
            };

            RunScope(scp, false);
            return scp;

            [DebuggerHidden]
            void RunCheck()
            {
                prev = curr;
                curr = wf();
            }

            [DebuggerHidden]
            void RunEffect()
            {
                if (!EqualityComparer<T>.Default.Equals(curr, prev))
                {
                    effect.Invoke();
                }
            }
        }

        public static Computed<T> Compute<T>(Func<T> getter, ScopeArgument argument = default)
        {
            AssertMainThread();
            return Compute(getter, out _, argument);
        }

        public static Computed<T> Compute<T>(Func<T> getter, out WatchScope scp, ScopeArgument argument = default)
        {
            AssertMainThread();
#pragma warning disable CS0618
            var computed = new Computed<T>(getter);
#pragma warning restore CS0618
            scp = new WatchScope(computed.Update, argument);
            computed.SetScope(scp);
            return computed;
        }

        public static WatchScope Watch<T>(IValuedData<T> wf, Action effect, ScopeArgument argument = default) => Watch(wf.GetValue, effect, argument);

        public static WatchScope Watch<T>(IValuedData<T> wf, Action<T> effect, ScopeArgument argument = default) => Watch(wf.GetValue, effect, argument);

        public static WatchScope Watch<T>(IValuedData<T> wf, Action<T, T> effect, ScopeArgument argument = default) => Watch(wf.GetValue, effect, argument);

        [Conditional("DEBUG")]
        static void AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                throw new("WatchScope can only be created from main thread.");
            }
        }
    }

}
