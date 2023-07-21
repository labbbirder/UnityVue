using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
// using SetterDict = System.Collections.Generic.Dictionary<string,System.Action<object,string,object,object>>;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Profiling;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
// using System.Runtime.CompilerServices;
// using Collections.Pooled;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace com.bbbirder
{
    public abstract partial class CSReactive
    {
        static int MAX_ITER_COUNT = 100;
        public enum FlushMode{
            /// <summary>
            /// 数据变化时立即更新
            /// </summary>
            /// <typeparam name="bool"></typeparam>
            Immediate,
            /// <summary>
            /// 数据变化后，当前帧LateUpdate更新（默认）
            /// </summary>
            /// <typeparam name="bool"></typeparam>
            LateUpdate,
        }
        public class WatchScope
        {
            // readonly int MAX_ITER_COUNT = 200;
            public FlushMode flushMode {get;internal set;}= FlushMode.LateUpdate;
            public string name="< unset >";
            internal bool isStacking = false;
            internal bool dirty = false;
            bool Inited = false;
            public Action effect;
            public Action normalEffect;
            public Func<bool> lifeCheckFunc;
            public HashSet<HashSet<WatchScope>> deps = new();
            // public PooledDictionary<IWatched,PooledSet<string>> deps = new();
            // public Dictionary<IWatched, Action<object,string,object,object>> setters = new();
            public WatchScope(Action effect):this(effect,null) {
            }
            public WatchScope(Action effect,Action normalEffect) { 
                this.effect = effect; 
                this.normalEffect = normalEffect;
                #if UNITY_EDITOR
                if(!ScopeKeeper.Instance&&!Inited) {
                    Inited = true;
                    EditorApplication.update+=UpdateDirtyScopes;
                }
                com.bbbirder.unity.ScopeVisualizer.Instance.refs.Add(new(this));
                #else
                if(!ScopeKeeper.Instance) throw new Exception("没有找到ScopeKeeper单例");

                #endif
            }
            /// <summary>
            /// 添加数据依赖
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="name"></param>
            /// <returns>是否成功，已存在则不成功</returns>
            // public bool AddDeps(IWatched obj,string name){
            //     var isSuccess = false;
            //     if(!deps.ContainsKey(obj)){
            //         deps[obj] = new();
            //     }
            //     isSuccess = !deps[obj].Contains(name);
            //     deps[obj].Add(name);
            //     return isSuccess;
            // }
            // public bool HasDeps(IWatched obj,string key){
            //     return deps.ContainsKey(obj) && deps[obj].Contains(key);
            // }
            internal void SetDirty(){
                dirty = true;
                willDirtyScopes.Add(this);
            }
            /// <summary>
            /// bind lifecycle with a reference, auto deactive when target reference is collected.
            /// </summary>
            /// <typeparam name="bool"></typeparam>
            public WatchScope WithRef<T>(T data) where T:class{
                //Note that: According to Unity Manual, Unity Object with WeakReference is not supported.
                if(data is UnityEngine.Object uo){ 
                    lifeCheckFunc = ()=>uo;
                }else{
                    var wr = new WeakReference<T>(data);
                    lifeCheckFunc = ()=>{
                        var ret = wr.TryGetTarget(out var target);
                        return ret && target!=null;
                    };
                }
                return this;
            }
            public void RunEffect(bool invokeNormalEffect = true)
            {
                dirty = false;
                if(lifeCheckFunc!=null&&lifeCheckFunc()==false){
                    ClearDependencies();
                    return;
                }
                isStacking = true;
                // current scope is already 
                //if (scopeStack.TryPeek(out var top) && top == this) return;
                if (scopeStackDepth >= MAX_ITER_COUNT) return; //TODO: Bag Iter

                
                //clear dependentings
                ClearDependencies();

                //collect dependenting data
                scopeStackDepth+=1;
                activeScope = this;
                // Profiler.BeginSample("RunEffect");
                effect();
                // Profiler.EndSample();
                activeScope = null;
                scopeStackDepth-=1;

                if(invokeNormalEffect) normalEffect?.Invoke();


                
                isStacking = false;
                
                void ClearDependencies(){
                    foreach (var dep in deps)
                    {
                        // var obj = dep.Key;
                        // var set = dep.Value;
                        // foreach(var k in set){
                        //     GetWatchScopes(obj,k).Remove(this);
                        // }
                        dep.Remove(this);
                        // GetWatchScopes(obj).Remove(this);
                        // set.Clear();
                    }
                    deps.Clear();
                }
            }
            
            public static void UpdateDirtyScopes(){
                var iter_count = 0;
                while(dirtyScopes.Count+willDirtyScopes.Count>0){
                    foreach(var scp in willDirtyScopes){
                        dirtyScopes.Add(scp);
                    }
                    willDirtyScopes.Clear();

                    iter_count++;
                    if(iter_count>MAX_ITER_COUNT){
                        dirtyScopes.Clear();
                        Debug.LogWarning("effect times exceed max iter count");
                        return;
                    }
                    
                    foreach(var scp in dirtyScopes){
                        // scp.dirty = false;
                        scp.RunEffect();
                    }
                    dirtyScopes.RemoveWhere(scp=>!scp.dirty);
                }
                // // dataDeps.
                // foreach(var kvp in dataDeps){
                // //     kvp.Value.Clear();
                // //     // kvp.Value.RemoveWhere(e=>!e.deps.ContainsKey(kvp.Key));
                // }
            }
        }
        internal static int scopeStackDepth = 0;
        internal static WatchScope activeScope = null;
        // public static HashSet<WatchScope> scopeStack = new();
        internal static HashSet<WatchScope> dirtyScopes = new();
        internal static HashSet<WatchScope> willDirtyScopes = new();
        public struct DataAccess{
            public object obj;
            public string name;
        }
        public static DataAccess lastAccess = new ();
        public static WatchScope WatchEffect(Action effect
            ,Func<bool> lifeCheck=null
            ,FlushMode flushMode=FlushMode.LateUpdate,
            [CallerMemberName]string callerName=null)
        {
            var scp = new WatchScope(effect);
            scp.lifeCheckFunc = lifeCheck;
            scp.flushMode = flushMode;
            scp.name = callerName;
            scp.RunEffect();
            return scp;
        }
        public static WatchScope WatchEffect(Func<object> effect)
        {
            return WatchEffect(() => { effect(); });
        }
        public static WatchScope Watch<T>(Func<T> wf,Action<T,T> effect){
            T prev=default,curr=default;
            var scp = new WatchScope(
                () => { prev=curr;curr=wf(); },
                () => effect(curr,prev)
            );
            scp.RunEffect(false);
            return scp;
        }
        public static WatchScope Watch<T>(Func<T> wf,Func<object,T,T> effect){
            return Watch(wf,(c,p) => { effect(c,p); });
        }
        public static WatchScope Watch<T>(Func<T> wf,Action<T> effect){
            return Watch(wf,(c,_) => { effect(c); });
        }
        public static WatchScope Watch<T>(Func<T> wf,Func<object,T> effect){
            return Watch(wf,(c) => { effect(c); });
        }
        public static WatchScope Watch<T>(Func<T> wf,Action effect){
            return Watch(wf,(c,_) => { effect(); });
        }
        public static WatchScope Watch<T>(Func<T> wf,Func<object> effect){
            return Watch(wf,() => { effect(); });
        }
        public static WatchScope Compute<T>(Func<T> expf,Action<T> setf){
            T t = default;
            var scp = new WatchScope(()=>{t=expf();},() => { setf(t); });
            scp.RunEffect();
            return scp;
        }
        //public static T Make<T>(T o)
        //{ 
        //    throw new NotImplementedException($"{typeof(T)}你没有给这个类添加OnChange或者未正常生成");
        //}

        // public static __Internal_DataMaker DataMaker => __Internal_DataMaker.Instance;
        // public class __Internal_DataMaker
        // {
        //     static __Internal_DataMaker _instance;
        //     public static __Internal_DataMaker Instance => _instance ?? new();
        //     public Ref<T> Ref<T>(T t){
        //         var r = new Ref<T>(t);
        //         DataWatcher.OnMakeData(r);
        //         return r;
        //     }
        // }
    }

    public interface IWatched {
        public Action<object, string> onSetProperty { get; set; }
        public Action<object, string> onGetProperty { get; set; }
        public void __InitWithRawData(object raw);
    }
    // public static partial class DataExtension
    // {
    //     public static T Make<T>(this DataWatcher.__Internal_DataMaker _, T __)
    //     {
    //         throw new NotImplementedException($"{typeof(T)}你没有给这个类添加OnChange或者未正常生成");
    //     }
    // }

}