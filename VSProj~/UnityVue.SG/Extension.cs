using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace UnityVue.SG
{
    internal static class Extensions
    {


        public static string GetSimpleName(this INamespaceSymbol ns)
        {
            if (ns.IsGlobalNamespace)
            {
                return "";
            }
            return ns.ToString();
        }

        public static bool IsFullNameEquals<T>(this INamedTypeSymbol type)
        {

            return type.ContainingNamespace.GetSimpleName() == typeof(T).Namespace
                && type.Name == typeof(T).Name
                ;
        }

        public static string GetFullName(this INamedTypeSymbol type)
        {
            var typesChain = type.GetContainingTypesIncludingSelf().Reverse().Select(t => t.Name);

            var ns = type.ContainingNamespace.GetSimpleName();
            if (string.IsNullOrEmpty(ns))
            {
                return ns + "." + string.Join(".", typesChain);
            }
            else
            {
                return string.Join(".", typesChain);
            }
        }

        static IEnumerable<INamedTypeSymbol> GetContainingTypesIncludingSelf(this INamedTypeSymbol type)
        {

            while(type != null)
            {
                yield return type;
                type = type.ContainingType;
            }
        }

    }
}
