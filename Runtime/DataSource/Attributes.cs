using System;
namespace BBBirder.UnityVue
{
    [Flags]
    public enum AccessibilityLevel
    {
        None = 0,
        Private = 1,
        Internal = 2,
        Public = 4,
        All = -1
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ExportFieldsAttribute : Attribute
    {
        public AccessibilityLevel AccessibilityLevel { get; set; } = AccessibilityLevel.All;
        public string MatchName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ExportIgnoreAttribute : Attribute { }
}
