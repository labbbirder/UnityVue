using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static string GetFullName(this ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        }

        //public static string GetFullName(INamedTypeSymbol type)
        //{
        //    var typesChain = type.GetContainingTypesIncludingSelf().Reverse();

        //    var ns = typesChain.First().ContainingNamespace.GetSimpleName();

        //    if (!string.IsNullOrEmpty(ns))
        //    {
        //        return ns + "." + string.Join(".", typesChain.Select(t=>t.Name));
        //    }
        //    else
        //    {
        //        return string.Join(".", typesChain.Select(t => t.Name));
        //    }
        //}

        public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol type, bool includeSelf = true)
        {
            if (!includeSelf)
            {
                type = type?.BaseType;
            }

            while (type != null)
            {
                yield return type;
                type = type.ContainingType;
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol type, bool includeSelf = true)
        {
            if (!includeSelf)
            {
                type = type?.BaseType;
            }

            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        public static bool IsInternalAccessible(this INamedTypeSymbol type)
        {
            foreach (var declType in type.GetContainingTypes())
            {
                if (declType.DeclaredAccessibility < Accessibility.Internal) return false;
            }
            return true;
        }

        public static bool IsTypeOrSubTypeOf<T>(this ITypeSymbol symbol)
        {
            if (typeof(T).IsInterface)
            {
                foreach (var interf in symbol.AllInterfaces)
                {
                    if (interf is INamedTypeSymbol namedType && namedType.IsFullNameEquals<T>()) return true;
                }
            }
            else
            {
                foreach (var baseType in symbol.GetBaseTypes())
                {
                    if (baseType is INamedTypeSymbol namedType && namedType.IsFullNameEquals<T>()) return true;
                }
            }
            return false;
        }

        public static IEnumerable<T> GetAttributes<T>(this ISymbol type) where T : Attribute
        {
            return type.GetAttributes().Where(a => a.AttributeClass.IsFullNameEquals<T>()).Select(ToAttribute<T>);
        }

        public static T GetAttribute<T>(this ISymbol type) where T : Attribute
        {
            return type.GetAttributes<T>().FirstOrDefault();
        }

        private static T ToAttribute<T>(this AttributeData data) where T : Attribute
        {
            if (data is null) return null;
            var constructorArguments = data.ConstructorArguments.Select(a => a.Value).ToArray();
            var attribute = Activator.CreateInstance(typeof(T), constructorArguments);
            foreach (var pair in data.NamedArguments)
            {
                var name = pair.Key;
                var value = pair.Value.Value;
                attribute.GetType().GetProperty(name)?.SetValue(attribute, value);
                attribute.GetType().GetField(name)?.SetValue(attribute, value);
            }
            return attribute as T;
        }


    }
}
