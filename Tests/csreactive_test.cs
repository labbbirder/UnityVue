using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static com.bbbirder.CSReactive;
using com.bbbirder;
using System;
using System.Linq;

public class csreactive_test
{
    [Watchable]
    public class Data2{
        public int vv;
    }
    [Watchable]
    public class Data1{
        public int v = 2;
        public string s = "asd";
        public float f = 0.2f;
        public List<Data2> d2list = new();
    }
    // A Test behaves as an ordinary method
    [Test]
    public void csreactive_testSimplePasses()
    {
        var data = DataMaker.Reactive(new Data1());
        var effect_cnt = 0;
        var v = 0;
        WatchEffect(()=>{
            effect_cnt+=1;
            v = data.v+1;
        });
        data.v = 111;
        CSReactive.WatchScope.UpdateDirtyScopes();
        Assert.AreEqual(112,v);
        Assert.AreEqual(2,effect_cnt);
    }
    [Test]
    public void csreactive_testRelevantPasses()
    {
        var data = DataMaker.Reactive(new Data1());
        var effect_cnt = 0;
        var f = 0.0f;
        WatchEffect(()=>{
            if(data.v<10)data.f=data.v;
        });
        WatchEffect(()=>{
            effect_cnt+=1;
            f = data.f;
        });
        for (int i = 0; i < 20; i++)
        {
            data.v = i;
            CSReactive.WatchScope.UpdateDirtyScopes();
        }
        Assert.AreEqual(11,effect_cnt);
        Assert.AreEqual(9,f);
    }
    [Test]
    public void csreactive_testLimit(){
        var data = DataMaker.Reactive(new Data1());
        var effect_cnt1 = 0;
        WatchEffect(()=>{
            data.v+=1;
            effect_cnt1+=1;
        });
        CSReactive.WatchScope.UpdateDirtyScopes();
        Debug.Log(effect_cnt1);
        Assert.LessOrEqual(effect_cnt1,300);
        Assert.GreaterOrEqual(effect_cnt1,100);
        var effect_cnt2 = 0;
        WatchEffect(()=>{
            data.f = data.v+1;
            effect_cnt2+=1;
        });
        WatchEffect(()=>{
            data.v = ((int)data.f+1);
            effect_cnt2+=1;
        });
        data.v+=1;
        CSReactive.WatchScope.UpdateDirtyScopes();
        Debug.Log(effect_cnt2);
        Assert.LessOrEqual(effect_cnt2,300);
        Assert.GreaterOrEqual(effect_cnt2,100);

        
        var effect_cnt3 = 0;
        WatchEffect(()=>{
            data.v+=1;
            effect_cnt3+=1;
        },flushMode:FlushMode.Immediate);
        Debug.Log(effect_cnt3);
        Assert.LessOrEqual(effect_cnt3,300);
        Assert.GreaterOrEqual(effect_cnt3,100);

    }
    WeakReference csreactive_testGC_inner(){
        var data = DataMaker.Reactive(new Data1());
        int flags = 0;
        var effect_cnt = 0;
        WatchScope scp = null; 
        scp = WatchEffect(()=>{
            effect_cnt+=1;
            // if(flags<=3){
                flags = data.v;
            // }
            Debug.Log($"has dep:{scp?.HasDeps(data,"v")},{flags}");
        },flushMode:FlushMode.Immediate);
        for (int i = 0; i < 60; i++)
        {
            data.v = i;
        }
        // Assert.AreEqual(7,effect_cnt);
        // Assert.AreEqual(4,flags);
        data.v = 123;
        // Assert.AreEqual(7,effect_cnt);
        scp.deps.Clear();
        var wr = new WeakReference(scp);
        return wr;
    }
    [Test]
    public void csreactive_testGC0(){
        var wr = csreactive_testGC_inner();
        GC.Collect();
        Assert.AreNotEqual(null,wr.Target);
    }
    [Test]
    public void csreactive_testGC1(){
        var wr = csreactive_testGC_inner();
        lastAccess.obj = null; //remove the last reference to data
        GC.Collect();
        Assert.AreEqual(null,wr.Target);
    }
}
