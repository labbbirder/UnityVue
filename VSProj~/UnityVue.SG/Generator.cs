using BBBirder.UnityVue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityVue.SG
{
    public struct DeclarationInfo
    {
        public struct NestInfo
        {
            public string keyword;
            public string name;
        }
        public struct MemberInfo
        {
            public string type;
            public string raw_type;
            public string name;
            public string raw_name;
            public string getter_accessibility;
            public string setter_accessibility;
            public bool is_watchable;
        }
        //public string target_name;
        public string target_namespace;
        //public string target_fullname;
        public string module_name;
        public MemberInfo[] members;
        public MemberInfo[] auto_impl_properties;
        public NestInfo[] declaringTypes;
    }

    [Generator]
    public class Generator : ISourceGenerator
    {
        private const string ValidAssemblyName = "BBBirder.UnityVue";
        private readonly DiagnosticDescriptor LowAccessibilityDescriptor = new(
            LowAccessibility.AnalyzerID,
            LowAccessibility.AnalyzerTitle,
            LowAccessibility.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);
        private readonly DiagnosticDescriptor NotGeneratedDescriptor = new(
            NotGenerated.AnalyzerID,
            NotGenerated.AnalyzerTitle,
            NotGenerated.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);

        private static DeclarationInfo.MemberInfo[] GetGeneratedProperties(INamedTypeSymbol type)
        {
            var attr = type.GetAttribute<ExportFieldsAttribute>();
            if (attr is null) return Array.Empty<DeclarationInfo.MemberInfo>();
            var reg = string.IsNullOrEmpty(attr.MatchName) ? null : new Regex(attr.MatchName);
            return type.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(m => m.CanBeReferencedByName && !m.IsImplicitlyDeclared)
                .Where(m => !m.IsStatic)
                .Where(m => reg is null || reg.IsMatch(m.Name))
                .Where(m => m.GetAttribute<ExportIgnoreAttribute>() == null)
                .Where(m => m.DeclaredAccessibility switch
                {
                    Accessibility.Private => (attr.AccessibilityLevel & AccessibilityLevel.Private) != 0,
                    Accessibility.Internal => (attr.AccessibilityLevel & AccessibilityLevel.Internal) != 0,
                    Accessibility.Public => (attr.AccessibilityLevel & AccessibilityLevel.Public) != 0,
                    _ => false,
                })
                .Where(m => !type.MemberNames.Contains(GetPropertyName(m.Name)))
                .Select(m => new DeclarationInfo.MemberInfo()
                {
                    name = GetPropertyName(m.Name),
                    raw_name = m.Name,
                    type = m.Type.GetFullName(),
                    raw_type = m.Type.GetFullName(),
                    is_watchable = m.Type.IsTypeOrSubTypeOf<IWatchable>(),
                })
                .ToArray()
                ;
            static string GetPropertyName(string fieldName)
            {
                var name = fieldName;
                if (name.StartsWith("m_"))
                {
                    name = name.Substring(2);
                }
                else if (name.StartsWith("_"))
                {
                    name = name.Substring(1);
                }
                return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
            }
        }

        internal DeclarationInfo GetDeclarationInfo(GeneratorExecutionContext context, TypeDeclarationSyntax typeSyntax)
        {
            var model = context.Compilation.GetSemanticModel(typeSyntax.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(typeSyntax);

            var declaringTypes = new List<DeclarationInfo.NestInfo>();
            var declaringType = symbol;
            while (declaringType != null)
            {
                var keyword = (declaringType.DeclaringSyntaxReferences[0].GetSyntax() as TypeDeclarationSyntax).Keyword.ToString();
                declaringTypes.Insert(0, new()
                {
                    keyword = keyword,
                    name = declaringType.Name
                });
                declaringType = declaringType.ContainingType;
            }

            var members = GetGeneratedProperties(symbol);
            return new()
            {
                target_namespace = symbol.ContainingNamespace.GetSimpleName(),
                module_name = symbol.ContainingModule.ToDisplayString(),
                members = members,
                declaringTypes = declaringTypes.ToArray(),
            };
        }

        private static Accessibility GetMinAccessiblityInContainingTypes(ISymbol symbol)
        {
            var acc = symbol.DeclaredAccessibility;
            if (symbol.ContainingType != null)
            {
                var acc2 = GetMinAccessiblityInContainingTypes(symbol.ContainingType);
                if (acc2 < acc)
                {
                    acc = acc2;
                }
            }
            return acc;
        }

        private static bool HasInterface(GeneratorExecutionContext context, TypeDeclarationSyntax t)
        {
            if (t.BaseList is null) return false;
            //var thisType = context.Compilation.GetSemanticModel(t.SyntaxTree).GetDeclaredSymbol(t);
            foreach (var b in t.BaseList.Types)
            {
                var type = context.Compilation.GetSemanticModel(b.SyntaxTree).GetSymbolInfo(b.Type).Symbol as INamedTypeSymbol;
                if (type is null) continue;
                if (type.IsFullNameEquals<IDataProxy>())
                {
                    return true;
                }
            }
            return false;
        }

        //private static bool HasAbstractToken(GeneratorExecutionContext _, TypeDeclarationSyntax t)
        //{
        //    return t.Modifiers.Any(SyntaxKind.AbstractKeyword);
        //}

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            var refs = context.Compilation.ReferencedAssemblyNames.Select(n => n.Name).ToArray();
            if (!refs.Contains(ValidAssemblyName) && context.Compilation.AssemblyName != ValidAssemblyName) return;

            var receiver = context.SyntaxReceiver as Receiver;

            var implTemplate = Template.Parse(Templates.ImplementInterfaceTemplate);
            var fieldsTemplate = Template.Parse(Templates.ExportFieldsTemplate);
            try
            {
                // Generate properties from exported fields
                Dictionary<INamedTypeSymbol, DeclarationInfo.MemberInfo[]> exportedFields = new();
                foreach (var type in receiver.ExportFieldList)
                {
                    var s = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type);
                    if (exportedFields.ContainsKey(s)) continue;

                    exportedFields.Add(s, Array.Empty<DeclarationInfo.MemberInfo>());
                    if (s.GetAttribute<ExportFieldsAttribute>() == null)
                    {
                        continue;
                    }
                    else
                    {
                        var info = GetDeclarationInfo(context, type);
                        exportedFields[s] = info.members;
                        var content = fieldsTemplate.Render(info);
                        context.AddSource($"{s.GetFullName()}-fields.g.cs", content);
                    }
                }

                // Generate auto implimented members for IDataProxy
                var validTypes = receiver.ImplementInterfaceList
                    .Where(d => HasInterface(context, d))
                    .ToArray()
                    ;
                foreach (var type in validTypes)
                {
                    var s = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type);
                    if (GetMinAccessiblityInContainingTypes(s) < Accessibility.Internal)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(LowAccessibilityDescriptor, type.GetLocation(), s.Name));
                        continue;
                    }
                    var implMethods = s.GetMembers().Select(m => m.Name).ToImmutableHashSet();
                    var info = GetDeclarationInfo(context, type);
                    var interfaceProperties = s.AllInterfaces.Single(i => i.IsFullNameEquals<IWatchable>())
                        .GetMembers()
                        .OfType<IPropertySymbol>();
                    info.auto_impl_properties = interfaceProperties
                        .Where(p => !implMethods.Contains(p.Name))
                        .Select(p => new DeclarationInfo.MemberInfo()
                        {
                            name = p.Name,
                            type = p.Type.GetFullName(),
                        })
                        .ToArray();
                    if (!exportedFields.TryGetValue(s, out var exported))
                    {
                        exported = Array.Empty<DeclarationInfo.MemberInfo>();
                    }

                    info.members = s.GetMembers().OfType<IPropertySymbol>()
                        .Where(p => p.SetMethod != null && p.GetMethod != null)
                        .Select(p => new DeclarationInfo.MemberInfo()
                        {
                            name = p.Name,
                            is_watchable = p.Type.IsTypeOrSubTypeOf<IWatchable>(),
                        })
                        .Concat(exported)
                        .Where(p => p.is_watchable)
                        .ToArray()
                        ;
                    var content = implTemplate.Render(info);
                    context.AddSource($"{s.GetFullName()}-impl.g.cs", content);
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(NotGeneratedDescriptor, null, e.ToString()));
            }

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }
    }
}
