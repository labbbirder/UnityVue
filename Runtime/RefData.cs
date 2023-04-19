using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace com.bbbirder
{

    public class __RefData<T>:IWatched{
        T __rawObject;
        public T value{
            get {
                onGetProperty?.Invoke(this,"value");
                return __rawObject;
            }
            set{
                if(EqualityComparer<T>.Default.Equals(__rawObject,value)) return;
                __rawObject = value;
                onSetProperty?.Invoke(this,"value");
            }
        }
        internal __RefData(T t){
            __rawObject = t;
        }
        public Action<object, string> onSetProperty { get; set; }
        public Action<object, string> onGetProperty { get; set; }
        public void __InitWithRawData(object raw) {
            __rawObject = (T)raw;
        }
    }
}
