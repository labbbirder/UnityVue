
using System;
using BBBirder.UnityVue;

Console.WriteLine("Hello world");
namespace com.asd
{
    namespace DDd
    {
        public partial record Root
        {
            internal partial class Foo : IDataProxy
            {
                void Test()
                {
                }
            }
            class Bar : Foo { }
        }

    }
}