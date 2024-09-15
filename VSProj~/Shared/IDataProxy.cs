using System;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public interface IWatchable
    {
        int SyncId { get; set; }
        bool IsProxyInited { get; set; }
        Action<object> onPropertySet { get; set; }
        Action<object> onPropertyGet { get; set; }
    }

    public partial interface IDataProxy : IWatchable
    {
        void VisitWatchableMembers(Action<IWatchable> visitor);
    }
}