using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBBirder.UnityVue
{
    public static class UnityObjectExtensions
    {
        [DebuggerHidden]
        public static WatchScope WatchEffect<TComp>(this TComp component,
            Action effect)
            where TComp : Object
        {
            var scp = CSReactive.WatchEffect(effect).WithLifeKeeper(component);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action<T, T> effect)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithLifeKeeper(component);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action<T> effect)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithLifeKeeper(component);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action effect)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithLifeKeeper(component);
            return scp;
        }

        [DebuggerHidden]
        public static WatchScope Compute<TComp, T>(this TComp component,
            Func<T> expf, Action<T> setf)
            where TComp : Object
        {
            var scp = CSReactive.Compute(expf, setf).WithLifeKeeper(component);
            return scp;
        }

        internal static T GetOrAddComponent<T>(this GameObject gameObject) where T : MonoBehaviour
        {
            var component = gameObject.GetComponent<T>();
            if (!component)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}
