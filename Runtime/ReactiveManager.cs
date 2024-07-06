using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BBBirder.UnityVue
{

    public class UnityReferenceLifeKeeper : ReferenceLifeKeeper
    {
        public UnityEngine.Object uo;
        public override bool IsAlive => uo;
        public UnityReferenceLifeKeeper(UnityEngine.Object refer)
        {
            uo = refer;
        }
    }

    public class WeakReferenceLifeKeeper : ReferenceLifeKeeper
    {

        public WeakReference wr;
        public override bool IsAlive => wr.IsAlive && wr.Target != null;
        public WeakReferenceLifeKeeper(object refer)
        {
            wr = new(refer);
        }
    }

    public abstract class ReferenceLifeKeeper : IScopeLifeKeeper
    {
        public abstract bool IsAlive { get; }
        public event Action onDestroy;
        internal void DestroyImmediate()
        {
            onDestroy?.Invoke();
            onDestroy = null;
        }
        internal static ReferenceLifeKeeper Create(object obj)
        {
            var uobjType = typeof(UnityEngine.Object);
            var isUnityObject = obj.GetType().IsSubclassOf(uobjType) || obj.GetType() == uobjType;
            if (isUnityObject)
            {
                return new UnityReferenceLifeKeeper((UnityEngine.Object)obj);
            }
            else
            {
                return new WeakReferenceLifeKeeper(obj);
            }
        }
    }


    public class ReactiveManager : MonoBehaviour
    {
        static ReactiveManager _instance;
        private Dictionary<object, ReferenceLifeKeeper> referenceLifeKeepers = new();
        public static ReactiveManager Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = Resources.FindObjectsOfTypeAll<ReactiveManager>().FirstOrDefault();
                }
                return _instance;
            }
        }
        public Action onUpdate = CSReactive.UpdateDirtyScopes;

        public ReferenceLifeKeeper GetReferenceLifeKeeper<T>(T refer) where T : class
        {
            if (!referenceLifeKeepers.TryGetValue(refer, out var keeper))
            {
                referenceLifeKeepers[refer] = keeper = ReferenceLifeKeeper.Create(refer);
            }
            return keeper;
        }

        void Awake()
        {
            if (_instance && _instance != this)
            {
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this);
                return;
            }
            if (!_instance) _instance = this;
        }

        static Stack<object> deadReferences;
        void Update()
        {
            deadReferences ??= new();

            foreach (var (refer, keeper) in referenceLifeKeepers)
            {
                if (!keeper.IsAlive)
                {
                    keeper.DestroyImmediate();
                    deadReferences.Push(refer);
                }
            }

            foreach (var refer in deadReferences)
            {
                referenceLifeKeepers.Remove(refer);
            }
            deadReferences.Clear();
        }

        void LateUpdate()
        {
            onUpdate?.Invoke();
        }
    }
}
