using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.bbbirder;
using Sirenix.OdinInspector;
using UnityEngine;


public class VisualWeakReferences : MonoBehaviour
{
    #if UNITY_EDITOR
    static VisualWeakReferences m_Instance;
    public static VisualWeakReferences Instance{
        get {
            if(!m_Instance){
                var go = new GameObject(nameof(VisualWeakReferences));
                go.hideFlags = HideFlags.DontSave;
                m_Instance = go.AddComponent<VisualWeakReferences>();
            }
            return m_Instance;
        }
    }
    [Button("Clear")]
    public void ClearNullRefs(){
        Instance.refs.RemoveAll(wr=>wr.wr.Target==null);
    }
    [ReadOnly]
    public List<VisualWR> refs = new();
    [HideReferenceObjectPicker,Serializable]
    public class VisualWR
    {
        internal WeakReference wr;
        CSReactive.WatchScope scope=>wr?.Target as CSReactive.WatchScope;
        // [ShowInInspector]
        public string keys => string.Join(",",scope.deps.SelectMany(dep=>dep.Value));
        public VisualWR(object target)
        {
            wr = new(target);
            VisualWeakReferences.Instance.refs.Add(this);
        }
    }
    #endif
}
