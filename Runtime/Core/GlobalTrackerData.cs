
using System;

namespace BBBirder.UnityVue
{
    public partial class GlobalTrackerData
    {
        public static readonly GlobalTrackerData Instance = new();
#if ENABLE_UNITY_VUE_TRACKER
        public bool Enabled { get; private set; } = true;
        public RList<WeakReference<WatchScope>> scopes { get; set; } = new();
        static GlobalTrackerData()
        {
            CSReactive.Reactive(Instance);
        }
#else
        public bool Enabled { get; private set; } = false;
#endif
    }

#if ENABLE_UNITY_VUE_TRACKER
    partial class GlobalTrackerData : IDataProxy { }
#endif

}