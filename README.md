# csreactive
Unity纯C#版的VUE，运行时0GC。
### FEATURES
实现了几乎所有VUE特性
* 理论上支持全平台
* 运行时无GC
* 数据变化时，对相关作用域做脏标记。当前帧的LateUpdate，对脏作用域进行刷新。
* 出现循环读写情况时，控制台会输出警告
* 支持List、Array等数组成员
* 默认的延迟更新和立即更新两种flush mode

其他功能
* 数据可视化调试
* 作用域可视化调试
* 响应式GameObject和组件
* 作用域可以与任意一个引用类型绑定，同生命周期

限制
* 数据源不支持字典、集合等其他容器，可以实现后提PR
### INSTALL
PackageManager下用git url安装：https://github.com/labbbirder/CSReactive.git
### QUICK START
添加命名空间
```csharp
using com.bbbirder;
using static com.bbbirder.CSReactive;
```

定义一个类型
```csharp
namespace YourNamespace{
  [Watchable]
  public class YourData{
    public string name;
  }
}
```

实现响应式
```csharp
var data = DataMaker.Reactive(new YourNamespcae.YourData());
var txtTips = "";
WatchEffect(()=>{
  txtTips = $"hello,{data.name}."
});
data.name = "bbbirder";
```
### NOTES
DynamicExpresso.dll并非官方原版，不要盲目更新它。此程序集在官方的基础上做了一些改动，已使其支持WebGL
