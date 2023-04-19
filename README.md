# csreactive
Unity纯C#版的VUE，运行时0GC。
### FEATURES
实现了几乎所有VUE特性
* 运行时无GC
* 数据变化时，对相关作用域做脏标记。当前帧的LateUpdate，对脏作用域进行刷新。
* 出现循环读写情况时，控制台会输出警告
* 支持List、Array等数组成员
### INSTALL
PackageManager下用过git url安装：https://github.com/labbbirder/CSReactive.git
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
