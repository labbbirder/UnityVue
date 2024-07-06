// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

// namespace com.bbbirder
// {

//     public static class WatchedExtension
//     {
//         static BindingFlags InstanceFlags = 0
//             | BindingFlags.Instance
//             | BindingFlags.NonPublic
//             | BindingFlags.Public
//             ;
//         static HashSet<string> excludeCopyingProperty = new(){
//             "RawData","__rawObject",
//         };
//         public static void PopulateFromJson(this IDataProxy watched, string json)
//         {
//             var jobj = JObject.Parse(json);
//             PopulateFromJson(watched, jobj);
//         }
//         internal static bool HasWatchableAttribute(this Type type)
//         {
//             return type.GetCustomAttribute<WatchableAttribute>() != null;
//         }
//         internal static bool IsWatched(this Type type)
//         {
//             return typeof(IDataProxy).IsAssignableFrom(type);
//         }
//         internal static bool IsReactiveList(this Type type)
//         {
//             return type.GetGenericTypeDefinition() == typeof(ReactiveList<>);
//         }
//         // private static void SetValue(this object value,string key, object value){

//         // }
//         static (bool, object) GetMemberValue(this object target, string key)
//         {
//             var type = target.GetType();
//             var members = type.GetMember(key, InstanceFlags);
//             foreach (var mem in members)
//             {
//                 if (mem is PropertyInfo prop)
//                 {
//                     return (true, prop.GetValue(target));
//                 }
//                 if (mem is FieldInfo field)
//                 {
//                     return (true, field.GetValue(target));
//                 }
//             }
//             return (false, null);
//         }
//         static Type GetMemberType(this object target, string key)
//         {
//             var type = target.GetType();
//             var members = type.GetMember(key, InstanceFlags);
//             foreach (var mem in members)
//             {
//                 if (mem is PropertyInfo prop)
//                 {
//                     return prop.PropertyType;
//                 }
//                 if (mem is FieldInfo field)
//                 {
//                     return field.FieldType;
//                 }
//             }
//             return null;
//         }
//         static object CreateDefault(Type type)
//         {
//             return System.Activator.CreateInstance(type);
//         }
//         static bool IsFromType<T>(this Type type) where T : class
//         {
//             return typeof(T).IsAssignableFrom(type);
//         }
//         static void SetMemberValue(this object target, string key, object value)
//         {
//             var type = target.GetType();
//             var members = type.GetMember(key, InstanceFlags);
//             foreach (var mem in members)
//             {
//                 if (mem is PropertyInfo prop)
//                 {
//                     prop.SetValue(target, value);
//                     return;
//                 }
//                 if (mem is FieldInfo field)
//                 {
//                     field.SetValue(target, value);
//                     return;
//                 }
//             }
//         }
//         // public static ReactiveJson Reactive(this CSReactive.__Internal_Maker __maker, JObject obj){
//         //     return __maker.GetOrCreateWatchedFor<ReactiveJson>(obj);
//         // }
//         // public static ReactiveJson Reactive(this CSReactive.__Internal_Maker __maker, JArray obj){
//         //     return __maker.GetOrCreateWatchedFor<ReactiveJson>(obj);
//         // }
//         // static void LoadFromJson(object target, JObject jobj)
//         // {
//         //     var type = target.GetType();
//         //     foreach (var (key, token) in jobj)
//         //     {
//         //         if (token is JArray arr)
//         //         {
//         //             var memType = target.GetMemberType(key);
//         //             if(memType.IsFromType<IList>()){

//         //             }
//         //             var (has,mem) = target.GetMemberValue(key);
//         //             if(mem is IList list){
//         //                 list.Clear();
//         //                 foreach(var ele in arr){
//         //                     if(list.GetType().GetElementType().IsPrimitive){
//         //                         list.
//         //                     }else{

//         //                     }
//         //                 }
//         //             }
//         //             if (target.GetMemberValue)
//         //         }
//         //     }
//         // }
//         static void PopulateFromJson(this IDataProxy watched, JObject jobj)
//         {
//             var targetType = watched.GetType();
//             foreach (var prop in targetType.GetProperties())
//             {
//                 var name = prop.Name;
//                 var type = prop.PropertyType;
//                 if (excludeCopyingProperty.Contains(name))
//                     continue;
//                 if (!jobj.TryGetValue(name, out var jmem))
//                     continue;

//                 if (typeof(IDataProxy).IsAssignableFrom(type))
//                 {
//                     if (jmem is JObject jo)
//                     {
//                         var p = prop.GetValue(watched) as IDataProxy;
//                         if (p != null)
//                             p.PopulateFromJson(jo);
//                         else
//                             throw new NotImplementedException();
//                         return;
//                     }

//                     if (jmem is JArray ja && type.IsReactiveList())
//                     {
//                         var p = prop.GetValue(watched) as IList;
//                         p.Clear();
//                         throw new NotImplementedException();
//                     }
//                 }
//                 else
//                 {
//                     var val = TypeUtils.CastTo(jmem, type);
//                     prop.SetValue(watched, val);
//                 }
//             }
//         }

//         public static T Reactive<T>(this CSReactive.InternalMaker maker, T t) where T : IDataProxy
//         {
//             return maker.SetProxy(t);
//         }

//         // public static T GetOrCreateWatchedFor<T>(this IWatched self, object watchable) where T : IWatched, new()
//         // {
//         //     return CSReactive.DataMaker.GetOrCreateWatchedFor<T>(watchable);
//         // }
//     }

// }
