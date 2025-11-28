using System.Collections.Generic;
using BBBirder.UnityInjection;
using BBBirder.UnityVue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CollectionTest : IPrebuildSetup
{
    [Test]
    public void List_Add_Should_Emit_Count_And_Element()
    {
        var group = new CubeGroup();
        var count = -1;
        var volume0 = 0f;
        CSReactive.WatchEffect(() =>
        {
            if (group.cubeList.Count > 0)
            {
                var c = group.cubeList[0];
                volume0 = c.Length * c.Width * c.Height;
            }
            else
            {
                volume0 = 0;
            }
        }, ScopeFlushMode.Immediate);

        CSReactive.Watch(
            () => group.cubeList.Count,
            c => count = c,
            ScopeFlushMode.Immediate);

        var list = group.cubeList;
        var cubeA = group.cubeA;

        list.Add(group.cubeA);
        Assert.AreEqual(group.cubeA, list[0]);
        Assert.AreEqual(group.cubeList, list);
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(1, count);

        cubeA.Width = cubeA.Height = cubeA.Length = 2;
        Assert.AreEqual(8, volume0);

        Debug.Log(list);

        list.Insert(0, new()
        {
            Width = 2,
            Height = 3,
            Length = 7,
        });
        Assert.AreEqual(2, count);
        Assert.AreEqual(2 * 3 * 7, volume0);

        Debug.Log(list);
        list.RemoveAt(0);
        Debug.Log(list);
        Assert.AreEqual(1, count);
        Assert.AreEqual(2, list[0].Length);
        Assert.AreEqual(8, volume0);

        list.Clear();
        Assert.AreEqual(0, count);
        Assert.AreEqual(0, volume0);
    }

    [Test]
    public void List_AddRange_Should_Emit_Count_Elements()
    {
        var group = new CubeGroup()
        {
            cubeList = new()
            {
                new(),
                new(),
                new(),
            }
        };
        var evts = new List<string>();
        // ((IWatchable)group).onPropertySet += evts.Add;
        var vSum = 0f;
        var count = 0;

        CSReactive.WatchEffect(() =>
        {
            var sum = 0f;
            foreach (var cube in group.cubeList)
            {
                sum += cube.Volume;
            }

            vSum = sum;
        }, ScopeFlushMode.Immediate);

        CSReactive.Watch(
            () => group.cubeList.Count,
            c => count = c,
            ScopeFlushMode.Immediate);

        group.cubeList.AddRange(new Cube[] { new() { Volume = 1 }, new() { Volume = 2 } });
        Assert.AreEqual(5, group.cubeList.Count);
        Assert.AreEqual(5, count);
        Assert.AreEqual(3, vSum);
    }

    [Test]
    public void Dictionary_Count_Should_Emit_On_Change()
    {
        var dict = new RDictionary<string, string>();
        var cnt = -1;
        CSReactive.WatchEffect(() =>
        {
            cnt = dict.Count;
        }, ScopeFlushMode.Immediate);
        Assert.AreEqual(0, cnt);
        dict["a"] = "a";
        Assert.AreEqual(1, cnt);
        dict["b"] = "b";
        Assert.AreEqual(2, cnt);
        dict["a"] = "b";
        Assert.AreEqual(2, cnt);
        dict.Add("c", "c");
        Assert.AreEqual(3, cnt);
        dict.Remove("a");
        Assert.AreEqual(2, cnt);
        dict.Clear();
        Assert.AreEqual(0, cnt);
        dict["Count"] = "111";
        Assert.AreEqual(1, cnt);
    }

    [Test]
    public void Dictionary_Item_Should_Emit_On_Change()
    {
        var dict = new RDictionary<string, string>();
        var value = "";
        CSReactive.WatchEffect(() =>
        {
            if (!dict.TryGetValue("a", out value))
            {
                value = null;
            }
        }, ScopeFlushMode.Immediate);

        dict["a"] = "a";
        Assert.AreEqual("a", value);

        dict.Remove("a");
        Assert.AreEqual(null, value);

        dict.Add("a", "asd");
        Assert.AreEqual("asd", value);

        dict.Clear();
        Assert.AreEqual(null, value);
    }

    [Test]
    public void Set_Count_Should_Emit_On_Change()
    {
        var set = new RSet<string>();
        var cnt = -1;
        CSReactive.WatchEffect(() =>
        {
            cnt = set.Count;
        }, ScopeFlushMode.Immediate);

        Assert.AreEqual(0, cnt);

        set.Add("a");
        Assert.AreEqual(1, cnt);

        set.Add("b");
        Assert.AreEqual(2, cnt);

        set.Add("a");
        Assert.AreEqual(2, cnt);

        set.Remove("a");
        Assert.AreEqual(1, cnt);

        set.Clear();
        Assert.AreEqual(0, cnt);
    }

    [Test]
    public void Set_Item_Should_Emit_On_Change()
    {
        var set = new RSet<string>();
        var aInSet = false;
        CSReactive.WatchEffect(() =>
        {
            aInSet = set.Contains("a");
        }, ScopeFlushMode.Immediate);

        set.Add("abc");
        Assert.AreEqual(false, aInSet);

        set.Remove("a");
        Assert.AreEqual(false, aInSet);

        set.Add("a");
        Assert.AreEqual(true, aInSet);

        set.Clear();
        Assert.AreEqual(false, aInSet);
    }

    public void Setup()
    {
        InjectionDriver.Instance.InstallAllAssemblies();
    }
}
