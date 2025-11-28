using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace BBBirder.UnityVue
{
    public interface IDataBinder : IScopeLifeKeeper
    {
        bool IsBinded { get; set; }
        // List<WatchScope> ScopesFromMember { get; }
        void OnBind();
        void OnUnbind();

        void Bind()
        {
            if (IsBinded) return;

            IsBinded = true;
            OnBind();

            var scp = default(WatchScope);
            scp = this.Watch(() => IsEnabled, v =>
            {
                if (v)
                {
                    scp.Dispose();
                    foreach (var a in this.GetBindAttributes())
                    {
                        a.Enable(this);
                    }
                }
            });
            scp.Update();
        }

        void Unbind()
        {
            if (!IsBinded) return;

            IsBinded = false;
            OnUnbind();
            this.ReleaseScopes();
        }
    }
}
