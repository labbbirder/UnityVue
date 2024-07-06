using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BBBirder.UnityVue;
using BBBirder.UnityInjection;

internal partial class Cube : IDataProxy
{
    public string Name { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Intensity { get; set; }

    public float Volume { get; set; }
    public float Mass { get; set; }
    public float Area { get; set; }
}

internal partial class CubeGroup : IDataProxy
{
    public float Extend { get; set; }
    public Cube cubeA { get; set; } = new();
    public Cube cubeB { get; set; } = new();

    public RList<Cube> cubeList { get; set; } = new();
}

public partial class BasicTest : IPrebuildSetup
{
    [Test]
    public void Cube_Area_Should_Be_Auto_Computed()
    {
        var cube = CSReactive.Reactive(new Cube());

        CSReactive.WatchEffect(() =>
        {
            var halfArea = cube.Length * cube.Width + cube.Length * cube.Height + cube.Width * cube.Height;
            cube.Area = halfArea * 2;
        }).WithArguments(ScopeFlushMode.Immediate);

        cube.Length = 2;
        cube.Width = 3;
        cube.Height = 5;
        Assert.AreEqual((2 * 3 + 3 * 5 + 2 * 5) * 2, cube.Area);

        cube.Length = 7;
        Assert.AreEqual((7 * 3 + 3 * 5 + 7 * 5) * 2, cube.Area);
    }


    [Test]
    public void Cube_Mass_Should_Be_Computed_Recursively()
    {
        var cube = CSReactive.Reactive(new Cube());

        CSReactive.WatchEffect(() =>
        {
            cube.Volume = cube.Length * cube.Width * cube.Height;
        }).WithArguments(ScopeFlushMode.Immediate);

        CSReactive.WatchEffect(() =>
        {
            cube.Mass = cube.Volume * cube.Intensity;
        }).WithArguments(ScopeFlushMode.Immediate);

        cube.Length = 2;
        cube.Width = 3;
        cube.Height = 5;
        cube.Intensity = 0.1f;
        Assert.AreEqual(2 * 3 * 5 * 0.1f, cube.Mass);

        cube.Length = 7;
        Assert.AreEqual(7 * 3 * 5 * 0.1f, cube.Mass);

        cube.Intensity = 1;
        Assert.AreEqual(7 * 3 * 5 * 1, cube.Mass);
    }

    [Test]
    public void Recursive_Update_Should_Be_Limited()
    {
        var cube = CSReactive.Reactive(new Cube());

        CSReactive.WatchEffect(() =>
        {
            cube.Area++;
        }).WithArguments((
            flushMode: ScopeFlushMode.Immediate,
            updateLimit: 10
        ));

        Assert.AreEqual(10, cube.Area);

        CSReactive.WatchEffect(() =>
        {
            cube.Width = cube.Height + 1;
        }).WithArguments((
            flushMode: ScopeFlushMode.Immediate,
            updateLimit: 10
        ));

        CSReactive.WatchEffect(() =>
        {
            cube.Height = cube.Width + 1;
        }).WithArguments((
            flushMode: ScopeFlushMode.Immediate,
            updateLimit: 10
        ));

        Assert.AreEqual(19, cube.Width);
        Assert.AreEqual(20, cube.Height);

    }

    [Test]
    public void Scope_Should_Disposed_On_Notification_From_LifeKeeper()
    {
        var cube = CSReactive.Reactive(new Cube());

        var o = new GameObject();
        Assert.True(o);

        var scope = CSReactive.WatchEffect(() =>
        {
            cube.Width = cube.Height + 1;
        })
        .WithArguments(ScopeFlushMode.Immediate)
        .WithRef(o);

        cube.Height = 100;
        Assert.AreEqual(101, cube.Width);

        UnityEngine.Object.DestroyImmediate(o);
        Assert.False(o);
        Assert.False(scope.lifeKeeper.IsAlive);

        cube.Height = 200;
        Assert.AreEqual(101, cube.Width);
    }


    [Test]
    public void Child_Proxy_Should_Be_Watched_As_Well()
    {
        var cubeGroup = CSReactive.Reactive(new CubeGroup());

        var scope = CSReactive.WatchEffect(() =>
        {
            var e = cubeGroup.Extend;
            var a = cubeGroup.cubeA;
            cubeGroup.cubeA.Volume = (a.Length + e) * (a.Height + e) * (a.Width + e);
        }).WithArguments(ScopeFlushMode.Immediate);

        cubeGroup.cubeA.Width = 1;
        cubeGroup.cubeA.Height = 2;
        cubeGroup.cubeA.Length = 3;
        cubeGroup.Extend = 1;
        Assert.AreEqual((1 + 1) * (1 + 2) * (1 + 3), cubeGroup.cubeA.Volume);

        cubeGroup.cubeA.Width = 5;
        Assert.AreEqual((1 + 5) * (1 + 2) * (1 + 3), cubeGroup.cubeA.Volume);

        cubeGroup.cubeA = new Cube()
        {
            Width = 11,
            Height = 13,
            Length = 7,
        };
        Assert.AreEqual((1 + 7) * (1 + 11) * (1 + 13), cubeGroup.cubeA.Volume);
    }

    [Test]
    public void List_Add_One()
    {
        var cubeGroup = CSReactive.Reactive(new CubeGroup());

        var cnt = 0;
        var scope = CSReactive.WatchEffect(() =>
        {
            cnt = cubeGroup.cubeList.Count;
        }).WithArguments(ScopeFlushMode.Immediate);

    }

    public void Setup()
    {
        InjectionDriver.Instance.InstallAllAssemblies();
    }
}
