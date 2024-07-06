using System;

namespace BBBirder.UnityVue
{
    public interface IWatchable
    {
        bool IsProxyInited { get; set; }
        Action<object> onPropertySet { get; set; }
        Action<object> onPropertyGet { get; set; }
    }

    public interface IDataProxy : IWatchable { }
}