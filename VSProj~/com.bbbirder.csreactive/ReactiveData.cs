using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
namespace com.bbbirder{
    public class __ReactiveData<T> : DynamicObject,IWatched
    {
        Dictionary<string,object> innerData = new();
        Dictionary<string,Action<object>> setters = new();
        // Proxy proxy;
        public T rawObject;

        public Action<object, string> onSetProperty {get;set;}
        public Action<object, string> onGetProperty  {get;set;}

        public void OnInitData(){

        }
        public void AssignData<TData>(TData raw){
            var flags = 0
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance;
            foreach (var item in raw.GetType().GetFields(flags))
            {
                innerData[item.Name] = item.GetValue(raw);
                setters[item.Name] = o=>item.SetValue(raw,o);
            }
            foreach (var item in raw.GetType().GetProperties(flags))
            {
                try{
                    var setter = item.GetSetMethod();
                    if(setter!=null){
                        setters[item.Name] = o=>item.SetValue(raw,o);
                    }
                    innerData[item.Name] = item.GetValue(raw);
                }catch{}
            }
        }

        void SetValue(string key,object value){
            innerData.TryGetValue(key,out var prev);
            innerData[key] = value;
            if(setters.ContainsKey(key))setters[key]?.Invoke(value);
            onSetProperty?.Invoke(this,key);
        }
        object GetValue(string key){
            onGetProperty?.Invoke(this,key);
            //var value = innerData.GetValueOrDefault(key,null);
            innerData.TryGetValue(key, out var value);
            if(value == null||value.GetType().IsPrimitive||value is string){
                return value;
            }
            if(value is IWatched){
                return value;
            }
            
            // unwatched custom object
            var deepData = new __ReactiveData<object>(value);
            deepData.onGetProperty = onGetProperty;
            deepData.onSetProperty = onSetProperty;
            innerData[key] = deepData;
            return deepData;
        }

        public __ReactiveData(T raw){
            AssignData(raw);
            rawObject = raw;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetValue(binder.Name);
            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetValue(binder.Name,value);
            return true;
        }

        public void __InitWithRawData(object raw) {
            AssignData(raw);
            rawObject = (T)raw;
        }

        public object this[string name]{
            get{
                return GetValue(name);
            }
            set{
                SetValue(name,value);
            }
        }
        public static implicit operator T(__ReactiveData<T> data){
            return data.rawObject;
        }
    }


}
