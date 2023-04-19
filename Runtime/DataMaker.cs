using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
// using Collections.Pooled;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace com.bbbirder{

    public partial class CSReactive
    {
        // public class SmartInvoker{
        //     public void Invoke<T>(object obj,string name,T v,T pv){

        //     }
        //     // public void AddFunc<T>
        // }
        // internal static ConditionalWeakTable<IWatched,HashSet<WatchScope>> dataDeps;
        internal static ConditionalWeakTable<object,HashSet<WatchScope>> dataDeps;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<WatchScope> GetWatchScopes(IWatched obj){
            var objDict = dataDeps.GetOrCreateValue(obj);
            // if(!objDict.ContainsKey(key)) objDict.Add(key,new());
            return objDict;
        }

        public class __Internal_Maker
        {
            internal __Internal_Maker() { }
            // public T[] CreateArray<T>(T value)
            // {
            //     return new List<T>().ToArray();
            // }
            static internal Action<object,string> onGetMethod;
            static internal Action<object,string> onSetMethod;            
            static void SetupMethods(){
                dataDeps = new();
                var tempScopes = new WatchScope[4];
                // willDropDataDeps = new();
                onGetMethod = (object obj,string key)=>{
                    var watched = obj as IWatched;
                    Assert.IsNotNull(watched,"data should be IWatched");
                    // Profiler.BeginSample("update deps");
                    lastAccess.obj = watched;
                    lastAccess.name = key;
                    var scp = activeScope;
                    if(scp!=null){
                        scp.AddDeps(watched,key);
                        GetWatchScopes(watched).Add(scp);
                    }
                    // Profiler.EndSample();
                };
                var willDrop = new HashSet<WatchScope>(4);
                onSetMethod = (object obj,string key)=>{
                    var watched = obj as IWatched;
                    Assert.IsNotNull(watched,"data should be IWatched");

                    // willDrop.Clear();
                    var relevantScopes = GetWatchScopes(watched);//.AsEnumerable();
                    if(relevantScopes.Count>tempScopes.Length){
                        tempScopes = new WatchScope[relevantScopes.Count];
                    }
                    relevantScopes.CopyTo(tempScopes);
                    // foreach(var scp in relevantScopes){
                    for(int i = 0;i<relevantScopes.Count;i++){
                        var scp = tempScopes[i];
                        if(scp.deps.ContainsKey(watched)){
                            if(scp.deps[watched].Contains(key)){
                                if(scp.flushMode==FlushMode.Immediate){
                                    scp.RunEffect();
                                    // if(!scp.HasDeps(watched,key)){
                                    //     willDrop.Add(scp);
                                    // }
                                }else if(scp.flushMode==FlushMode.LateUpdate){
                                    scp.SetDirty();
                                }
                            }
                        }else{
                            relevantScopes.Remove(scp);
                        }
                        tempScopes[i] = null;
                    }
                    // relevantScopes.RemoveWhere(e=>!e.HasDeps(watched,key));
                };
            }
            public T OnMakeData<T>(T watched) where T : IWatched
            {
                // PooledSet<WatchScope>
                // Dictionary<string,HashSet<WatchScope>> 
                // var relevantScopes = GetWatchScopes(watched,);
                if(onGetMethod==null||onSetMethod==null){
                    SetupMethods();
                }
                watched.onGetProperty += onGetMethod;
                watched.onSetProperty += onSetMethod;
                return watched;
            }
            // [Obsolete("此方法在IL2CPP下不可用,请关注相关Unity官方更新。也可通过修改editor下源码实现。")]
            // public dynamic ReactiveDynamic<T>(T data)
            // {
            //     return OnMakeData(new __ReactiveData<T>(data));
            // }
            //public T Reactive<T>(T raw)
            //{
            //    throw new Exception($"生成失败，或未给类型{typeof(T).Name}添加Watchable属性");
            //}
            public __RefData<T> Ref<T>(T value)
            {
                return OnMakeData(new __RefData<T>(value));
            }
        }
        public static readonly __Internal_Maker DataMaker = new();

    }
}
