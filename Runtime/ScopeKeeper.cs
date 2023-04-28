using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace com.bbbirder{

    [ExecuteInEditMode]
    public class ScopeKeeper : MonoBehaviour
    {
        public static ScopeKeeper Instance;
        public Action onUpdate = CSReactive.WatchScope.UpdateDirtyScopes;
        
        void Awake(){
            Instance = this;
        }
        void LateUpdate()
        {
            onUpdate?.Invoke();
        }
    }
}
