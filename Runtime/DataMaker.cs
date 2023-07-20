using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
// using Collections.Pooled;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace com.bbbirder{

    public partial class CSReactive
    {
        // #if UNITY_2023_3_OR_NEWER
        // See: https://issuetracker.unity3d.com/issues/crashes-on-garbagecollector-collectincremental-when-entering-the-play-mode
        // Implement(IL2CPP): https://unity.com/releases/editor/whats-new/2021.2.1
        // Fixed(IL2CPP): https://unity.com/releases/editor/beta/2023.1.0b11
        // internal static ConditionalWeakTable<IWatched,Dictionary<string,HashSet<WatchScope>>> dataDeps;
        // #endif

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static HashSet<WatchScope> GetWatchScopes(IWatched obj, string key){
        //     var objDict = dataDeps.GetOrCreateValue(obj);
        //     if(!objDict.ContainsKey(key)) objDict.Add(key,new());
        //     return objDict[key];
        // }

        public class __Internal_Maker
        {
            internal __Internal_Maker() { }
            // static internal Action<object,string> onGetMethod;
            // static internal Action<object,string> onSetMethod;
            // 因为Unity对CWT支持有问题，所以这里使用Upvalue保存依赖表，代价是较多的内存分配
            static void SetupMethods(IWatched data){
                var dataDeps = new Dictionary<string,HashSet<WatchScope>>();
                var tempScopes = new WatchScope[4];
                data.onGetProperty = (object obj,string key)=>{
                    var watched = obj as IWatched;
                    Assert.IsNotNull(watched,"data should be IWatched");
                    // Profiler.BeginSample("update deps");
                    lastAccess.obj = watched;
                    lastAccess.name = key;
                    var scp = activeScope;
                    if(scp!=null){
                        var relevantScopes = GetWatchScopes(key);
                        relevantScopes.Add(scp);
                        scp.deps.Add(relevantScopes);
                    }
                    // Profiler.EndSample();
                };
                data.onSetProperty = (object obj,string key)=>{
                    var watched = obj as IWatched;
                    Assert.IsNotNull(watched,"data should be IWatched");

                    var relevantScopes = GetWatchScopes(key);
                    if(relevantScopes.Count>tempScopes.Length){
                        tempScopes = new WatchScope[relevantScopes.Count];
                    }
                    relevantScopes.CopyTo(tempScopes);
                    for(int i = 0;i<relevantScopes.Count;i++){
                        var scp = tempScopes[i];
                        if(scp.flushMode==FlushMode.Immediate){
                            //ISSUE : break tempscopes
                            scp.RunEffect();
                        }else if(scp.flushMode==FlushMode.LateUpdate){
                            scp.SetDirty();
                        }
                        tempScopes[i] = null;
                    }
                };
                HashSet<WatchScope> GetWatchScopes(string key){
                    if(!dataDeps.ContainsKey(key)){
                        dataDeps.Add(key,new());
                    }
                    return dataDeps[key];
                }
            }
            // static void SetupMethods(){
            //     dataDeps = new();
            //     CreateMethods();
            // }
            public T OnMakeData<T>(T watched) where T : IWatched
            {
                // watched.onGetProperty += onGetMethod;
                // watched.onSetProperty += onSetMethod;
                SetupMethods(watched);
                return watched;
            }
            // [Obsolete("此方法在IL2CPP下不可用,请关注相关Unity官方更新。也可通过修改editor下源码实现。")]
            // public dynamic ReactiveDynamic<T>(T data)
            // {
            //     return OnMakeData(new __ReactiveData<T>(data));
            // }
            // public object Reactive(object raw)
            // {
            //    throw new Exception($"生成失败，或未给类型{raw.GetType()}添加Watchable属性");
            // }
            public __RefData<T> Ref<T>(T value)
            {
                return OnMakeData(new __RefData<T>(value));
            }
        }
        public static readonly __Internal_Maker DataMaker = new();

    }
}
