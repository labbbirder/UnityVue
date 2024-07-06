# UnityVue
> 此库是本人接下来时间的开发维护重点，原名：CSReactive

Unity纯C#版的VUE，运行时0GC。
## FEATURES
实现了几乎所有VUE特性，自行简单封装后可以无缝对接FGUI、UGUI、UTK等几乎所有UI方案
* 支持全平台
* 运行时无GC
* 递归代理
* 数据变化时，对相关作用域做脏标记。当前帧的LateUpdate，对脏作用域进行刷新。
* 支持List、Array等数组成员
* 可自定义更新时机 : LateUpdate(post) & Immediate(sync)
* 代理函数: Reactive & Ref
* 绑定函数: Watch & Compute & WatchEffect & Bind
* MonoBehaviour响应式

其他功能
* 数据可视化调试 (支持OdinInspector)
* 作用域可视化调试
* 出现循环读写情况时，控制台会输出警告
* 响应式GameObject和组件
* 作用域可以与任意一个引用类型绑定，同生命周期

限制
* 数据源暂不支持字典、集合等其他容器，可以自行实现后提PR
## INSTALL
PackageManager下用git url安装：https://github.com/labbbirder/CSReactive.git
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
## NOTES
### DynamicExpresso
DynamicExpresso.dll并非官方原版，不要盲目更新它。此程序集在官方的基础上做了一些改动，已使其支持WebGL

DynamicExpresso可以用在响应式组件或GameObject上，如：在Inspector下，给一个Component的字段赋字符串表达式
### ConditionalWeakTable
如果此特性可用，为性能和内存最优解。但经测试和检阅资料发现主流Unity版本对此特性支持不佳（包括IL2CPP和MONO），因此使用临时方案实现，代价是较多的内存分配（创建数据代理时）。

