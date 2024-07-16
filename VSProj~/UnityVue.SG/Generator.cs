using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Scriban;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System;
using BBBirder.UnityVue;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Immutable;
using Scriban.Helpers;

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
        }
        //public string target_name;
        public string target_namespace;
        //public string target_fullname;
        public string module_name;
        public MemberInfo[] members;
        public NestInfo[] declaringTypes;
    }

    [Generator]
    public class Generator : ISourceGenerator
    {
        const string ValidAssemblyName = "BBBirder.UnityVue";

        readonly DiagnosticDescriptor LowAccessibilityDescriptor = new(
            LowAccessibility.AnalyzerID,
            LowAccessibility.AnalyzerTitle,
            LowAccessibility.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);

        readonly DiagnosticDescriptor NotGeneratedDescriptor = new(
            NotGenerated.AnalyzerID,
            NotGenerated.AnalyzerTitle,
            NotGenerated.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);

        static DeclarationInfo.MemberInfo[] GetMembers(INamedTypeSymbol type)
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
                })
                .ToArray()
                ;
            static string GetPropertyName(string fieldName)
            {
                return fieldName.Substring(0, 1).ToUpperInvariant() + fieldName.Substring(1);
            }
        }

        internal DeclarationInfo GetDeclarationInfo(GeneratorExecutionContext context, TypeDeclarationSyntax typeSyntax)
        {
            var model = context.Compilation.GetSemanticModel(typeSyntax.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(typeSyntax);

            var declaringTypes = new List<DeclarationInfo.NestInfo>();
            var declaringType = symbol;
            while(declaringType!=null)
            {
                var keyword = (declaringType.DeclaringSyntaxReferences[0].GetSyntax() as TypeDeclarationSyntax).Keyword.ToString();
                declaringTypes.Insert(0, new()
                {
                    keyword = keyword,
                    name = declaringType.Name
                });
                declaringType = declaringType.ContainingType;
            }

            var members = GetMembers(symbol);
            return new()
            {
                //target_name = symbol.Name,
                //target_fullname = symbol.GetFullName(),
                target_namespace = symbol.ContainingNamespace.GetSimpleName(),
                module_name = symbol.ContainingModule.ToDisplayString(),
                members = members,
                declaringTypes = declaringTypes.ToArray(),
            };
        }

        static Accessibility GetMinAccessiblityInContainingTypes(ISymbol symbol)
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

        static bool HasInterface(GeneratorExecutionContext context, TypeDeclarationSyntax t)
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


        static bool HasAbstractToken(GeneratorExecutionContext _, TypeDeclarationSyntax t)
        {
            return t.Modifiers.Any(SyntaxKind.AbstractKeyword);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            var refs = context.Compilation.ReferencedAssemblyNames.Select(n => n.Name).ToArray();
            if (!refs.Contains(ValidAssemblyName)) return;

            var receiver = context.SyntaxReceiver as Receiver;
            SyntaxNode visiting = null;
            try
            {

                var validTypes = receiver.ImplementInterfaceList
                    .Where(d => HasInterface(context, d))
                    //.Where(d => !HasAbstractToken(context, d))
                    .ToArray()
                    ;

                var implTemplate = Template.Parse(Templates.ImplementInterfaceTemplate);
                var fieldsTemplate = Template.Parse(Templates.ExportFieldsTemplate);
                var declareInfos = new Dictionary<string,string>();
                foreach (var type in validTypes)
                {
                    visiting = type;
                    var s = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type);
                    if (GetMinAccessiblityInContainingTypes(s) < Accessibility.Internal)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(LowAccessibilityDescriptor, type.GetLocation(), s.Name));
                        continue;
                    }
                    else
                    {
                        var implMethods = s.GetMembers().Select(m=>m.Name).ToImmutableHashSet();
                        var info = GetDeclarationInfo(context, type);
                        var interfaceProperties = s.AllInterfaces.Single(i => i.IsFullNameEquals<IWatchable>())
                            .GetMembers()
                            .OfType<IPropertySymbol>();
                        info.members = interfaceProperties
                            .Where(p => !implMethods.Contains(p.Name))
                            .Select(p => new DeclarationInfo.MemberInfo()
                            {
                                name = p.Name,
                                type = p.Type.GetFullName(),
                                //getter_accessibility = p.GetMethod.DeclaredAccessibility switch
                                //{
                                //     Accessibility.Internal=>"internal",
                                //     Accessibility.Protected=>"protected",
                                //     _=>"",
                                //},
                                //setter_accessibility = p.SetMethod.DeclaredAccessibility switch
                                //{
                                //    Accessibility.Internal => "internal",
                                //    Accessibility.Protected => "protected",
                                //    _ => "",
                                //}
                            })
                            .ToArray();
                        var content = implTemplate.Render(info);
                        context.AddSource($"{s.GetFullName()}-impl.g.cs",content);
                    }
                }
                foreach (var type in receiver.ExportFieldList)
                {
                    visiting = type;
                    var s = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type);
                    if (s.GetAttribute<ExportFieldsAttribute>() == null)
                    {
                        continue;
                    }
                    else
                    {
                        var content = fieldsTemplate.Render(GetDeclarationInfo(context, type));
                        context.AddSource($"{s.GetFullName()}-fields.g.cs", content);
                    }
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(NotGeneratedDescriptor, visiting.GetLocation(), e));
                try
                {
                }
                catch { }
                throw e;
            }

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }
    }
}
