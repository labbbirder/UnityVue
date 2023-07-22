using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace com.bbbirder{

    public class ReactiveManager : MonoBehaviour
    {
        public static ReactiveManager Instance;
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
