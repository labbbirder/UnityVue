using System;
using System.Collections.Generic;
using System.Linq;
using BBBirder.UnityVue;
using UnityEngine;

namespace BBBirder
{
    public partial class DataFlowGroup : ReactiveBehaviour
    {
        [SerializeField] bool autoBind;
        public ReactiveBehaviour[] involvedComponents;
        List<WatchScope> scopes = new();
        public override bool IsEnabled
        {
            get
            {
                foreach (var comp in involvedComponents)
                {
                    if (!comp.IsEnabled) return false;
                }

                return true;
            }
        }

        public override void OnBind()
        {
            DataFlowRegistry.BindAll(involvedComponents, this, scopes);
            // print(scopes.Count);
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnUnbind()
        {
            foreach (var scp in scopes)
            {
                scp.Dispose();
            }

            scopes.Clear();
        }

        public void Reset()
        {
            CollectionPool.Get<List<ReactiveBehaviour>>(out var reactiveBehaviours);
            GetComponentsInChildren(reactiveBehaviours);
            involvedComponents = reactiveBehaviours
                .Where(b => b && DataFlowRegistry.HasPipeEnds(b.GetType()))
                .ToArray();
        }

        internal void CollectComponents(bool includeChildren)
        {
            if (includeChildren)
            {
                involvedComponents = GetComponentsInChildren<ReactiveBehaviour>(true);
            }
            else
            {
                involvedComponents = GetComponents<ReactiveBehaviour>();
            }
        }
    }
}
