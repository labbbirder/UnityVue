using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace BBBirder.UnityVue
{
    public abstract partial class CSReactive
    {

#if UNITY_EDITOR
        public static void EditorUpdateDirtyScopes()
        {
            if (!Application.isPlaying) UpdateDirtyScopes();
        }
#endif
        [Obsolete("This is not a watchable type")]
        public static object Reactive(object watchable)
        {
            return null;
        }

        public static T Reactive<T>(T watchable) where T : IWatchable
        {
            return SetProxy(watchable);
        }

        public static RefData<T> Ref<T>(T data)
        {
            var refData = ObjectPool<RefData<T>>.Get();
            refData.Init(data);
            return new RefData<T>(data);
        }

        public static WatchScope WatchEffect(Action effect)
        {
            var scp = new WatchScope(effect);
            RunScope(scp);
            return scp;
        }

        public static WatchScope Watch<T>(Func<T> wf, Action<T, T> effect)
        {
            T prev = default, curr = default;
            var scp = new WatchScope(
                () => { prev = curr; curr = wf(); },
                () => effect(curr, prev)
            );
            RunScope(scp, false);
            return scp;
        }

        public static WatchScope Watch<T>(Func<T> wf, Action<T> effect)
        {
            return Watch(wf, (c, _) => { effect(c); });
        }

        public static WatchScope Watch<T>(Func<T> wf, Action effect)
        {
            return Watch(wf, (c, _) => { effect(); });
        }

        public static WatchScope Compute<T>(Func<T> expf, Action<T> setf)
        {
            T t = default;
            var scp = new WatchScope(() => { t = expf(); }, () => { setf(t); });
            RunScope(scp);
            return scp;
        }
    }

}
