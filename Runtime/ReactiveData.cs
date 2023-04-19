// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Dynamic;
// using System.Linq;
// using System.Reflection;
// using UnityEngine;
// namespace com.bbbirder{
//     public class __ReactiveData<T> : DynamicObject,IWatched
//     {
//         Dictionary<string,object> innerData = new();
//         Dictionary<string,Action<object>> setters = new();
//         Dictionary<string,bool> nonNested = new();
//         // Proxy proxy;
//         public T rawObject;

//         public Action<object, string> onSetProperty { get; set; }
//         public Action<object, string> onGetProperty { get; set; }

//         public void OnInitData(){
            
//         }
//         public void AssignData<TData>(TData raw){
//             var flags = 0
//             | BindingFlags.Public
//             | BindingFlags.NonPublic
//             | BindingFlags.Instance;
//             foreach (var item in raw.GetType().GetFields(flags))
//             {
//                 if(item.GetCustomAttribute<NonNestedAttribute>()!=null){
//                     nonNested[item.Name] = true;
//                 }
//                 innerData[item.Name] = item.GetValue(raw);
//                 setters[item.Name] = o=>item.SetValue(raw,o);
//             }
//             foreach (var item in raw.GetType().GetProperties(flags))
//             {
//                 try{
//                     if(item.GetCustomAttribute<NonNestedAttribute>()!=null){
//                         nonNested[item.Name] = true;
//                     }
//                     var setter = item.GetSetMethod();
//                     if(setter!=null){
//                         setters[item.Name] = o=>item.SetValue(raw,o);
//                     }
//                     innerData[item.Name] = item.GetValue(raw);
//                 }catch{}
//             }
//         }

//         void SetValue(string key,object value){
//             // var prev = innerData.GetValueOrDefault(key,null);
//             innerData[key] = value;
//             if(setters.ContainsKey(key))setters[key]?.Invoke(value);
//             onSetProperty?.Invoke(this,key);
//         }
//         public object ParseData(string key,object value){
//             var nest = true;
//             nest &= value is not null;
//             nest &= value is not int;
//             nest &= value is not float;
//             nest &= value is not string;
//             nest &= value is not double;
//             nest &= !value.GetType().IsPrimitive;
//             nest &= ! nonNested.ContainsKey(key);
//             if(!nest){
//                 return value;
//             }
//             if(value is IWatched){
//                 return value;
//             }
//             if(value is IEnumerable lstvalue){
//                 var rl = new ReactiveList<object>(lstvalue);
//                 rl.onGetProperty = onGetProperty;
//                 rl.onSetProperty = onSetProperty;
//                 return rl;
//             }
//             // unwatched custom object
//             var deepData = new __ReactiveData<object>(value);
//             deepData.onGetProperty = onGetProperty;
//             deepData.onSetProperty = onSetProperty;
//             return deepData;
//         }
//         object GetValue(string key){
//             onGetProperty?.Invoke(this,key);
//             var value = innerData.GetValueOrDefault(key,null);
//             // var nonNest = false;
//             // nonNest |= value==null;
//             // nonNest |= value is int;
//             // nonNest |= value is float;
//             // nonNest |= value is string;
//             // nonNest |= value is double;
//             // nonNest |= value.GetType().IsPrimitive;
//             // nonNest |= nonNested.ContainsKey(key);
//             // if(nonNest){
//             //     return value;
//             // }
//             // if(value is IWatched){
//             //     return value;
//             // }
//             // // unwatched custom object
//             // var deepData = new __ReactiveData<object>(value);
//             // deepData.onGetProperty = onGetProperty;
//             // deepData.onSetProperty = onSetProperty;
//             var new_value = ParseData(key,value);
//             innerData[key] = new_value;
//             return new_value;
//         }

//         public __ReactiveData(T raw){
//             AssignData(raw);
//             rawObject = raw;
//         }
//         public override bool TryGetMember(GetMemberBinder binder, out object result)
//         {
//             result = GetValue(binder.Name);
//             return true;
//         }
//         public override bool TrySetMember(SetMemberBinder binder, object value)
//         {
//             SetValue(binder.Name,value);
//             return true;
//         }
//         public object this[string name]{
//             get{
//                 return GetValue(name);
//             }
//             set{
//                 SetValue(name,value);
//             }
//         }
//         public static implicit operator T(__ReactiveData<T> data){
//             return data.rawObject;
//         }
//         public static implicit operator __ReactiveData<T>(T raw){
//             return new (raw);
//         }
        
//         public void __InitWithRawData(object raw) {
//             rawObject = (T)raw;
//         }
//     }


// }
