using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OnChange.SG {
    internal static class Extensions {
        public static bool IsIEnumerator(this ITypeSymbol type) {
            return type.SpecialType != SpecialType.System_String
                && type.AllInterfaces.Any(a => a.ToString() == "System.Collections.IEnumerable");
        }
        public static bool HasAttribute(this ITypeSymbol type, string attributeName) {
            return type.GetAttributes().Any(a => a.AttributeClass.ToString().Equals(attributeName));
        }
        public static bool IsWatched(this ITypeSymbol type) {
            return type.HasAttribute("com.bbbirder.WatchableAttribute");
        }
        public static string GetFullName(this ITypeSymbol type,bool isWatched) {
            var name = "";
            if (!type.ContainingNamespace.IsGlobalNamespace) {
                name += type.ContainingNamespace.ToString() + ".";
            }
            return name + type.Name;
        }
        public static string GetNSPart(this ITypeSymbol type) {
            var ns = type.ContainingNamespace;
            return ns.IsGlobalNamespace ? "" : (ns.ToString() + ".");
        }
        public static string GetNamePart(this ITypeSymbol type) {
            return type.Name;
        }
        public static string GetGenericPart(this ITypeSymbol type) {
            var res = "";
            if (type is INamedTypeSymbol nt && nt.TypeArguments.Length > 0) {
                res += $"<{string.Join(",", nt.TypeParameters)}>";
            }
            return res;
        }
        public enum NameParts{
            None = 0,
            Namespace = 1,
            Generic = 2,
        }
        public static string GetName(this ITypeSymbol type, NameParts parts, Func<string,string> rename = null)
        {
            var np = type.GetNamePart();
            var name = rename != null ? rename(np) : np;
            if (0!=(parts & NameParts.Namespace))
            {
                name = type.GetNSPart() + name;
            }
            if(0!=(parts & NameParts.Generic))
            {
                name = name + type.GetGenericPart();
            }
            return name;
        }
    }
}
