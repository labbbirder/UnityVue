using System;
using System.Collections.Generic;

namespace BBBirder.UnityVue
{
    public class WatchablePayload { }
    public interface IWatchable
    {
        WatchablePayload Payload { get; }
    }

    public partial interface IDataProxy : IWatchable
    {
        void VisitWatchableMembers(Action<IWatchable> visitor);
    }

    public class BindableAttribute : Attribute
    {
    }
}
