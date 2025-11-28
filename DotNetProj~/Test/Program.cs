
using BBBirder.UnityVue;
using com.asd.DDd;
using System;

var obj = new SkillData();

; (obj as IDataProxy).VisitWatchableMembers(m => Console.WriteLine(m));
Console.WriteLine("Hello world");
namespace com.asd
{
    namespace DDd
    {
        public partial record Root
        {
            internal partial class Foo : IDataProxy
            {
                int IWatchable.SyncId { get; set; } 

                internal string name;
                private Bar bar;
                private int age;
                private List<int>.Enumerator en;

            }
            internal class Bar : Foo { }
        }

        public partial class SkillData : IDataProxy
        {
            private float cooldown;
            private float cooldownMax;
            private BattlerData battler = new();
        }
        public partial class BattlerData : IDataProxy
        {

        }

    }
}
