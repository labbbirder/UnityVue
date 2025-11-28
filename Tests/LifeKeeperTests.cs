using System;
using System.Collections;
using System.Collections.Generic;
using BBBirder.UnityInjection;
using BBBirder.UnityVue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
class BehaviourTest : ReactiveBehaviour
{

}

class ScriptableObjectTest : ScriptableObject
{

}

public class LifeKeeperTests : IPrebuildSetup
{
    public static IEnumerable GetKeeperSources()
    {
        var goKeeper1 = new GameObject("LifeKeeper1")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        yield return new TestCaseData(goKeeper1.GetLifeKeeper(),// DestroyTrigger
            new Action(() => GameObject.DestroyImmediate(goKeeper1)),
            new Action<bool>(enabled => goKeeper1.SetActive(enabled))
        );

        var goKeeper2 = new GameObject("LifeKeeper2")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var behav = goKeeper2.AddComponent<BehaviourTest>();
        yield return new TestCaseData(behav, // ReactiveBehaviour
            new Action(() => GameObject.DestroyImmediate(behav)),
            new Action<bool>(enabled => behav.enabled = enabled)
        );

        var so = ScriptableObjectTest.CreateInstance<ScriptableObjectTest>();
        so.hideFlags = HideFlags.HideAndDontSave;
        yield return new TestCaseData(so.GetLifeKeeper(), // Polling
            new Action(() => GameObject.DestroyImmediate(so)),
            null
        );


    }

    [Test]
    [TestCaseSource(nameof(GetKeeperSources))]
    public void LifeKeeper_Enable_And_Destroy_Should_Works(IScopeLifeKeeper keeper, Action destroyer, Action<bool> enableSetter)
    {
        var cube = new Cube()
        {
            Width = 1,
            Height = 2,
            Length = 3,
        };

        var volume0 = 0f;
        keeper.WatchEffect(() =>
        {
            var c = cube;
            volume0 = c.Length * c.Width * c.Height;

        }, ScopeFlushMode.Immediate);

        Assert.AreEqual(6, volume0);

        cube.Width = cube.Height = cube.Length = 2;
        Assert.AreEqual(8, volume0);

        if (enableSetter != null)
        {
            enableSetter(false);

            cube.Width = cube.Height = cube.Length = 3;
            Assert.AreEqual(8, volume0);

            keeper.UpdateDirtyScopes();
            Assert.AreEqual(27, volume0);

            cube.Width = cube.Height = cube.Length = 2;

            enableSetter(true);
            Assert.AreEqual(8, volume0);
        }

        destroyer();

        UnityVueDriver.CheckAndRemoveDestroyedUnityObjectReferences();

        cube.Width = cube.Height = cube.Length = 3;
        Assert.AreEqual(8, volume0);
    }


    public void Setup()
    {
        InjectionDriver.Instance.InstallAllAssemblies();
    }
}
