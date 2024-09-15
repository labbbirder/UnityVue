using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace BBBirder.UnityVue
{
    public interface IDataBinder : IScopeLifeKeeper
    {
        bool IsBinded { get; }
        List<WatchScope> m_AttributeScopes { get; set; }

        [DebuggerHidden]
        void OnBindInternal()
        {
            OnBind();
            m_AttributeScopes ??= new();
            foreach (var a in this.GetBindAttributes())
            {
                a.Enable(this);
            }
        }

        void OnUnbindInternal()
        {
            if (m_AttributeScopes != null)
            {
                foreach (var scope in m_AttributeScopes)
                {
                    scope.Dispose();
                }
            }
            m_AttributeScopes?.Clear();
            OnUnbind();
        }

        void OnBind();
        void OnUnbind();
    }
}
