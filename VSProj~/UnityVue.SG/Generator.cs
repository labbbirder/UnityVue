using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Scriban;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System;
using BBBirder.UnityVue;

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
            public string cast_type;
            public string raw_type;
            public string name;
        }
        public string target_name;
        public string target_namespace;
        public string target_fullname;
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



        internal DeclarationInfo GetDeclarationInfo(GeneratorExecutionContext context, TypeDeclarationSyntax typeSyntax)
        {
            var model = context.Compilation.GetSemanticModel(typeSyntax.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(typeSyntax);

            var declaringTypes = new List<DeclarationInfo.NestInfo>();
            var declaringType = symbol.ContainingType;
            while(declaringType!=null)
            {
                var keyword = (declaringType.DeclaringSyntaxReferences[0].GetSyntax() as TypeDeclarationSyntax).Keyword.ToString();
                declaringTypes.Add(new()
                {
                    keyword = keyword,
                    name = declaringType.Name
                });
                declaringType = declaringType.ContainingType;
            }

            var members = new List<DeclarationInfo.MemberInfo>();
            return new()
            {
                target_name = symbol.Name,
                target_fullname = symbol.GetFullName(),
                target_namespace = symbol.ContainingNamespace.GetSimpleName(),
                module_name = symbol.ContainingModule.ToDisplayString(),
                members = members.ToArray(),
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
            foreach(var b in t.BaseList.Types)
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
            try
            {

                var validTypes = receiver.DeclarationList
                    .Where(d => HasInterface(context, d))
                    .Where(d => !HasAbstractToken(context, d))
                    .ToArray()
                    ;

                var declareInfos = new List<DeclarationInfo>();
                foreach(var type in validTypes)
                {
                    var s = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type);
                    if (GetMinAccessiblityInContainingTypes(s) < Accessibility.Internal)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(LowAccessibilityDescriptor, type.GetLocation(), s.Name));
                        continue;
                    }
                    else
                    {
                        declareInfos.Add(GetDeclarationInfo(context, type));
                    }
                }

                var template = Template.Parse(Templates.ImplementInterfaceTemplate);
                foreach (var info in declareInfos)
                {
                    var targetFileName = $"{info.target_fullname}.g.cs";
                    var content = template.Render(info);
                    context.AddSource(targetFileName, content);
                }

            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(NotGeneratedDescriptor, null, e));
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
