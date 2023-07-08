using Microsoft.CodeAnalysis;
using OnChange.SG;
using Scriban;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OnChange.SG
{
    public class Settings
    {
        public const string Namespace = "com.bbbirder.onchange";
        public const string IgnoreAttributeName = Namespace + ".IgnoreAttribute";
        public const string WatchableAttributeName = Namespace + ".WatchableAttribute";
        public static string[] BlackList = new[] {
            "string",
            "System.string",
            "object",
        };
    }
    internal enum GenerateMode
    {
        Default = 0,
        Flat = 1,
        Methods = 2,
    }

}


namespace com.bbbirder {
    public enum DataFlags {
        Default = 0,
        Flat = 1 << 0,
        IgnoreBaseType = 1 << 1,
        Methods = 1 << 2,
        DontWriteBack = 1 << 3,
        NonPublic = 1 << 4,
    }
    ///// <summary>
    ///// Dont wrap this member
    ///// </summary>
    //public class TailMemberAttribute : System.Attribute { }
    ///// <summary>
    ///// Wrap this member
    ///// </summary>
    //public class DeepMemberAttribute : System.Attribute { }
}