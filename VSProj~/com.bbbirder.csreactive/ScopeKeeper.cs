using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace com.bbbirder{

    public static class Extension{
        public static void AttachToKeeper(this CSReactive.WatchScope scp,ScopeKeeper keeper){
            keeper.onUpdate+=scp.OnUpdate;
        }
    }
    [ExecuteInEditMode]
    public class ScopeKeeper : MonoBehaviour
    {
        public static ScopeKeeper Instance;
        public Action onUpdate;

        // Update is called once per frame
        void LateUpdate()
        {
            onUpdate?.Invoke();
        }
        public void KeepMany(params Action[] args){
            args.ToList().ForEach(a=>CSReactive.WatchEffect(a).AttachToKeeper(this));
        }
    }
}
