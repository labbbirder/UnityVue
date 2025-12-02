# UnityVue

![mono support](http://img.shields.io/badge/Mono-support-green)
![il2cpp support](http://img.shields.io/badge/IL2CPP-support-green)
![GitHub last commit](http://img.shields.io/github/last-commit/labbbirder/UnityVue)
![GitHub package.json version](http://img.shields.io/github/package-json/v/labbbirder/UnityVue)

Unity纯C#版的VUE，运行时高效，运行时0GC。
## FEATURES
实现了几乎所有VUE特性，自行简单封装后可以无缝对接FGUI、UGUI、UTK等几乎所有UI方案
* 支持全平台
* 运行时无GC
* 递归代理
* 数据变化时，对相关作用域做脏标记。当前帧的Update结束后，对脏作用域进行刷新。
* 支持List、Array等数组成员
* 可自定义更新时机 : LateUpdate(post) & Immediate(sync)
* ~~代理函数: Reactive & Ref~~ (可监听数据不再需要初始化)
* 绑定函数: Watch & Compute & WatchEffect & Bind
* MonoBehaviour响应式

其他功能
* 数据可视化调试 (支持OdinInspector)
* 作用域可视化调试
* 出现循环读写情况时，控制台会输出警告
* 响应式GameObject和组件
* 作用域可以与任意一个引用类型绑定，同生命周期

## INSTALL

execute command line：

```bash
openupm add com.bbbirder.unity-vue
```

## QUICK START
添加命名空间
```csharp
using BBBirder.UnityVue;
```

定义一个类型
```csharp
using BBBirder.UnityVue;
using Sirenix.OdinInspector;
partial class CubeData : IDataProxy
{
    [ShowInInspector]
    public float Length { get; set; } = 1;
    [ShowInInspector]
    public float Width { get; set; } = 1;
    [ShowInInspector]
    public float Height { get; set; } = 1;

    [ShowInInspector]
    public float Sum { get; set; }

    [ShowInInspector]
    public float Area { get; set; }

    [ShowInInspector]
    public float Volume { get; set; }
}
```

实现响应式
```csharp
using UnityEngine;

[ExecuteAlways]
partial class CubeData : MonoBehaviour
{

    void Awake()
    {
        CSReactive.Reactive(this);

        this.WatchEffect(() =>
        {
            var halfArea = (Length * Width) + (Width * Height) + (Length * Height);
            Area = halfArea * 2;
        });

        this.WatchEffect(() =>
        {
            Volume = Length * Width * Height;
        });

        this.Compute(() => Length + Width + Height, v => Sum = v);
    }
}

```

## Major Concepts

### IScopeLifeKeeper

作用域的生命管理对象。比如，一个Component下有多个作用域，当Component销毁时，这些作用域也要一并销毁，那么，此Component就适合作为一个`IScopeLifeKeeper`。同理，一个UI实体类也可以作为一个`IScopeLifeKeeper`。

### ReactiveBehaviour

继承自MonoBehaviour，并提供了数据绑定接口。

```csharp
public class MyComp : ReactiveBehaviour
{
    public RefData<int> age = new();
    public RefData<int> ageNextYear = new();

    [Watch]
    public int __WatchAge
    {
        get => age;
        set => ageNextYear.Value = value + 1;
    }

    [Watch(ScopeFlushMode.Immediate)] // 指定更新时机
    public int __WriteNextYearAge
    {
        get => age;
        set => print($"on set age to {value}");
    }

    [Watch]
    // 使用WatchArgument`1包装类型获取上一个数值（previous value）
    public WatchArgument<int> __WatchAge 
    {
        get => age;
        set
        {
            var (curr,prev) = value;
            print($"age changed from {prev} to {curr}");
        }
    }

    // 修改age会间接触发此方法更新
    [WatchEffect]
    void __PrintAges()
    {
        print($"age next year is {ageNextYear.Value}");
    }

    public void Update()
    {
        if (Input.KeyDown(KeyCode.A))
        {
            age.Value++;
        }
    }
}

```

## NOTES
### DynamicExpresso
DynamicExpresso.dll并非官方原版，不要盲目更新它。此程序集在官方的基础上做了一些改动，已使其支持WebGL

DynamicExpresso可以用在响应式组件或GameObject上，如：在Inspector下，给一个Component的字段赋字符串表达式
