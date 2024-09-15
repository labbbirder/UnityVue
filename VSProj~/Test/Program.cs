
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
            [ExportFields(AccessibilityLevel = AccessibilityLevel.All)]
            internal partial class Foo : IDataProxy
            {

                internal string name;
                [ExportIgnore]
                private Bar bar;
                private int age;
                private List<int>.Enumerator en;

                private void Test()
                {
                    this.En = en;
                }
            }
            internal class Bar : Foo { }
        }

        [ExportFields]
        public partial class SkillData : IDataProxy
        {
            private float cooldown;
            private float cooldownMax;
            private BattlerData battler = new();
            private float deltaCD => Cooldown - CooldownMax;
        }
        public partial class BattlerData : IDataProxy
        {

        }

    }
}
