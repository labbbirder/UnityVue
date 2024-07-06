using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBBirder.UnityVue
{
    public static class UnityObjectExtensions
    {
        public static WatchScope WatchEffect<TComp>(this TComp component,
            Action effect,
            [CallerMemberName] string caller = null, [CallerLineNumber] int ln = 0)
            where TComp : Object
        {
            var scp = CSReactive.WatchEffect(effect).WithRef(component);
#if UNITY_EDITOR
            scp.name = $"{caller}:{ln}";
#endif
            return scp;
        }

        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action<T, T> effect,
            [CallerMemberName] string caller = null, [CallerLineNumber] int ln = 0)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithRef(component);
#if UNITY_EDITOR
            scp.name = $"{caller}:{ln}";
#endif
            return scp;
        }

        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action<T> effect,
            [CallerMemberName] string caller = null, [CallerLineNumber] int ln = 0)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithRef(component);
#if UNITY_EDITOR
            scp.name = $"{caller}:{ln}";
#endif
            return scp;
        }

        public static WatchScope Watch<TComp, T>(this TComp component,
            Func<T> wf, Action effect,
            [CallerMemberName] string caller = null, [CallerLineNumber] int ln = 0)
            where TComp : Object
        {
            var scp = CSReactive.Watch(wf, effect).WithRef(component);
#if UNITY_EDITOR
            scp.name = $"{caller}:{ln}";
#endif
            return scp;
        }

        public static WatchScope Compute<TComp, T>(this TComp component,
            Func<T> expf, Action<T> setf,
            [CallerMemberName] string caller = null, [CallerLineNumber] int ln = 0)
            where TComp : Object
        {
            var scp = CSReactive.Compute(expf, setf).WithRef(component);
#if UNITY_EDITOR
            scp.name = $"{caller}:{ln}";
#endif
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