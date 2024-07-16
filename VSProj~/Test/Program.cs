
using System;
using BBBirder.UnityVue;

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
                Bar bar;
                int age;
                List<int>.Enumerator en;
                void Test()
                {
                    this.En = en;
                }
            }
            internal class Bar : Foo { }
        }

        [ExportFields]
        public abstract partial class SkillData:IDataProxy
        {
            float cooldown;
            float cooldownMax;
            float deltaCD => Cooldown - CooldownMax;
        }


    }
}
