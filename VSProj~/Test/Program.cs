using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using com.bbb;

using System;
using static com.bbbirder.CSReactive;
using com.bbbirder;
using System.Reflection;
using nsb;
//Console.WriteLine("Hello, World!");
//Console.WriteLine(OnChangeObject.UseCount);

//Console.WriteLine(OnChangeObject.logs);
var a = new A();

//Console.WriteLine(wa.age);


var result = 0;

A Fpp1() {
    return new A();
}
//var data = DataMaker.Reactive(Fpp1());
//var wa = DataMaker.Reactive(new Player());
var doo = DataMaker.Reactive(new Doo());

//doo.ints.Add(1);

//Console.WriteLine(data.name);
//WatchEffect(() => {

//});
//WatchEffect(() =>
//{
//    Console.WriteLine($"effect:{data.age},{result}");
//    if (data.name == "bbbirder")
//    {
//        Console.WriteLine($"is bbbirder");
//        result = data.age * 10;
//        data.name = "asd";
//        data.name = "asd";
//        data.name = "asd";
//        data.name = "asd";
//        data.name = "asd";
//        data.name = "asd";
//        result = data.age * 11;
//    }
//});
//Console.WriteLine($"result:{result}");
//data.age = 10;
//Console.WriteLine($"age:{data.age}");
//Console.WriteLine($"result:{result}\n");
//Console.WriteLine($"set good");
//data.name = "good";
//Console.WriteLine($"result:{result}\n");
//Console.WriteLine($"set bbbirder");
//data.name = "bbbirder";
//Console.WriteLine($"result:{result}\n");
//var wsa = DataWatcher.Watch(new Class1());
public partial class Watched_A {

}
public class FFF {
    //public int aaa { get { return 0; } set { } } = 13;
    public void Make<T>(T t) {
    }
}
[Watchable]
public class Doo {
    /// <summary>
    /// 标题
    /// </summary>
    public string labe2l;
    /// <summary>
    /// 这个可以Doo ints
    /// asd
    /// </summary>
    public List<int> ints = new();
    public MyList<double> ml = new();
    public MyList<A> mal = new();
}
[Watchable]
public class F123 {

}

namespace com.bbb
{
    public class SA : A {
        public void  Foo(string name)
        {
        }
        public int Fo2o<T>(string name)
        {
            return 1;
        }
        public static int SFo2o<T>(string name)
        {
            return 1;
        }
    }
    [Watchable]
    partial class EMail { }
}
public class MyList<T> : IList<T> {
    public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(T item) {
        throw new NotImplementedException();
    }

    public void Clear() {
        throw new NotImplementedException();
    }

    public bool Contains(T item) {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator() {
        throw new NotImplementedException();
    }

    public int IndexOf(T item) {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item) {
        throw new NotImplementedException();
    }

    public bool Remove(T item) {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index) {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        throw new NotImplementedException();
    }
}
[Watchable]
public partial class A:C
{
    public string name = "aaa";
    public int age = 13;
    public string addr { get; set; } = "lz";
    public string root => "asd";
    //public Based.Class1 outter_man;
    public nsb.B b;
}
partial class A {
    public string another_name;
}
namespace nsb {
    public class Inn<T> { }
[Watchable]
public class B{
        public string addr = "lz";
    public Inn<double> Value { get; set; }
}
}
public class C
{
    public float c_value;
}




[WatchableAttribute]
    public class Weapon {
        public string desc;
    }
[Watchable]
public class Player
{
    public string name;
    public int age;
    //Based.BaseDqas ddd;
    public Weapon weapon;
    public SA sa;
}

//int i = 100;
//Task.Delay(100).ContinueWith((task) => i += 20);

//Stopwatch Stopwatch = Stopwatch.StartNew();
//while (Stopwatch.ElapsedMilliseconds < 1000) {
//    i = i + 1;
//    i = i - 1;
//}
//i *= 2;
//Console.WriteLine("main end" + i);