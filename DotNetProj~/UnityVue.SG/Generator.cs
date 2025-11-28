using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BBBirder.UnityVue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace UnityVue.SG
{
    public struct DeclarationInfo
    {
        public struct NestInfo
        {
            public string keyword;
            public string name;
        }
        public string target_namespace;
        public string module_name;
        public NestInfo[] declaring_types;
    }

    [Generator]
    public class Generator : IIncrementalGenerator
    {
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

        internal DeclarationInfo GetDeclarationInfo(INamedTypeSymbol type)
        {
            var declaringTypes = new List<DeclarationInfo.NestInfo>();
            var declaringType = type;
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

            return new()
            {
                target_namespace = type.ContainingNamespace.GetSimpleName(),
                module_name = type.ContainingModule.ToDisplayString(),
                declaring_types = declaringTypes.ToArray(),
            };
        }

        private static Accessibility GetMinAccessibilityInContainingTypes(ISymbol symbol)
        {
            var acc = symbol.DeclaredAccessibility;
            if (symbol.ContainingType != null)
            {
                var acc2 = GetMinAccessibilityInContainingTypes(symbol.ContainingType);
                if (acc2 < acc)
                {
                    acc = acc2;
                }
            }
            return acc;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            #warning TODO: test
            // var validAssemblyPipe = context.CompilationProvider
            //     .Select((comp, ct) =>
            //     {
            //         var type =  comp.GetTypesByMetadataName(typeof(IDataProxy).FullName!)
            //             .FirstOrDefault(t => t.ContainingAssembly.Name == typeof(IDataProxy).Assembly.GetName().Name);
            //         File.AppendAllLines("D:/asd.log",new[]
            //         {
            //             comp.AssemblyName,
            //             type.GetFullName()
            //         });
            //         return type;
            //     });

            var declsPipeline = context.SyntaxProvider.CreateSyntaxProvider((n, _) =>
                {
                    return n is SimpleBaseTypeSyntax baseTypeSyntax
                           && baseTypeSyntax.Type.ToString() == nameof(IDataProxy);
                }, (ctx, _) => ctx)
                // .Combine(validAssemblyPipe)
                // .Where(tp => tp.Right != null)
                .Select((ctx, ct) =>
                {
                    // var (ctx, itype) = tp;
                    var node = ctx.Node as SimpleBaseTypeSyntax;
                    var nodeType = ctx.SemanticModel.GetTypeInfo(node!.Type, ct).Type;
                    var declNode = nodeType is INamedTypeSymbol nts && nts.IsFullNameEquals<IDataProxy>()
                            // SymbolEqualityComparer.Default.Equals(nodeType, itype)
                        ? node.FirstAncestorOrSelf<TypeDeclarationSyntax>()
                        : null
                        ;
                    return declNode != null
                        ? ctx.SemanticModel.GetDeclaredSymbol(declNode)
                        : null
                        ;
                })
                .Where(t => t != null)
                ;

            context.RegisterSourceOutput(declsPipeline, (ctx, source) =>
            {
                var type  = source;

                try
                {
                    var implMember = type.GetMembers().FirstOrDefault(m => m.Name == nameof(IWatchable.Payload));
                    if (implMember != null)
                    {
                        return;
                    }

                    if (GetMinAccessibilityInContainingTypes(type) < Accessibility.Internal)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(LowAccessibilityDescriptor,
                            type.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(),
                            type.Name));
                        return;
                    }

                    var info = GetDeclarationInfo(type);

                    var implTemplate = Template.Parse(Templates.ImplementInterfaceTemplate);
                    var content = implTemplate.Render(info);
                    ctx.AddSource($"{type.GetFullName()}-impl-IDataProxy.g.cs", content);
                }
                catch (Exception e)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(NotGeneratedDescriptor, null, e.ToString()));
                }
            });
        }
    }
}
