using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OnChange.SG;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using Scriban;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace com.bbbirder.onchange
{
    public struct DeclarationInfo:IEquatable<DeclarationInfo> {
        public struct MemberInfo {
            public bool isTail;
            public bool canWrite;
            public string type;
            public string name;
            public bool isList;
            public string elementType;
            //public bool isWatchGeneric;
            public string comment;
            public bool isWatched;
            //public string modifiers;
        }
        public string target_name;
        public string target_ns_part;
        public string target_namespace;
        public string target_simplename;
        public string src_name;
        public bool write_back;
        public string modulename;
        public MemberInfo[] members;

        public bool Equals(DeclarationInfo other) {
            return target_name == other.target_name && modulename == other.modulename;
        }
    }
    [Generator]
    public class OnChangeGenerator : ISourceGenerator
    {
        internal static DiagnosticDescriptor RuleBadInvoke = new DiagnosticDescriptor("CSReactive", "调用错误", "不正常的调用", "ErrorInvocation", DiagnosticSeverity.Error, true);
        const string ValidAssemblyName = "com.bbbirder.csreactive";
        const string WatchableAttrName = "com.bbbirder.WatchableAttribute";
        public void GetMembers(ITypeSymbol symbol, Dictionary<string, ISymbol> result, bool withBaseType) {
            if (symbol.BaseType is null) return; //omit primate types
            if (withBaseType) {
                GetMembers(symbol.BaseType, result, withBaseType);
            }
            foreach (var member in symbol.GetMembers()) {
                result[member.Name] = member;
            }
        }
        public AttributeData GetAttributeData(INamedTypeSymbol symbol,string attributeName) {

            if (symbol == null) return null ;
            var attr = symbol.GetAttributes().Where(a => {
                var name = a.AttributeClass.ToString();
                return name == attributeName;
                }).FirstOrDefault();
            return attr;
        }
        internal DeclarationInfo? GetDeclarationInfo(GeneratorExecutionContext context, TypeDeclarationSyntax cds) {
            var model = GetModel(context, cds);
            var symbol = model.GetDeclaredSymbol(cds);
            var attr = GetAttributeData(symbol, WatchableAttrName);
            if (attr == null) return null;
            var flags = (DataFlags)attr.ConstructorArguments.First().Value;
            var f_write_back = true;
            //var f_write_back = (flags & DataFlags.DontWriteBack) == 0;
            var f_non_public = false;
            //var f_non_public = (flags & DataFlags.NonPublic) != 0;
            var memberDict = new Dictionary<string, ISymbol>();
            var memberList = new List<DeclarationInfo.MemberInfo>();
            GetMembers(symbol, memberDict, true);

            foreach(var member in memberDict.Values) {

                var canWrite = true;
                var isTail = true;
                var isWatched = false;
                var elementType = "";
                var isList = false;
                ITypeSymbol type = null;
                if (!f_non_public && member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.IsStatic) continue;
                if (member.IsImplicitlyDeclared) continue;

                if (member is IFieldSymbol fld) {
                    type = fld.Type;
                }else
                if(member is IPropertySymbol prop) {
                    canWrite = prop.SetMethod != null;
                    type = prop.Type;
                }
                else {
                    continue;
                }
                var typename = type.ToString();
                var namedType = type as INamedTypeSymbol;
                isWatched = namedType != null && type.OriginalDefinition.IsWatched();
                if (isWatched) {
                    isTail = false;
                }
                isList = type.IsIEnumerator();
                if (isList)
                {
                    var eleType = namedType?.TypeArguments.First() ?? (type as IArrayTypeSymbol).ElementType;
                    elementType = eleType.ToString();
                    if (eleType.IsWatched())
                    {
                        elementType = eleType.GetName(
                            OnChange.SG.Extensions.NameParts.Namespace |
                            OnChange.SG.Extensions.NameParts.Generic, 
                            s => "Watched" + s);
                    }
                }

                
                if (isWatched) {
                    var name = "";
                    if (!type.ContainingNamespace.IsGlobalNamespace) {
                        name += type.ContainingNamespace.ToString() + ".";
                    }
                    name += "Watched" + type.Name;
                    if (namedType.TypeArguments.Length > 0) {
                        
                        name += "<"+ string.Join(",",namedType.TypeArguments) +">";
                    }
                    typename = name;
                }


                // retrieve document comment
                var trivias = member.DeclaringSyntaxReferences
                    .Select(s => s.GetSyntax().FirstAncestorOrSelf<MemberDeclarationSyntax>())
                    .SelectMany(mds => mds.DescendantTrivia())
                    .Select(s => s.ToString().Trim())
                    .Where(s => s.StartsWith("//"))
                    .ToArray()
                    ;
                var comment = string.Join("\n", trivias);

                //var types = namedType.TypeArguments.ToString();
                memberList.Add(new DeclarationInfo.MemberInfo {
                    name = member.Name,
                    type = typename,
                    canWrite = canWrite,
                    isTail = isTail,
                    isList = isList,
                    elementType = elementType,
                    isWatched = isWatched,
                    comment = comment
                    //isWatchGeneric = 
                });
            }
            var targetname = "Watched" + symbol.Name;
            if(symbol is INamedTypeSymbol nt && nt.TypeArguments.Length>0) {
                targetname += $"<{string.Join(",", nt.TypeParameters)}>";
            }
            return new DeclarationInfo {
                src_name = symbol.ToString(),
                target_ns_part = symbol.ContainingNamespace.IsGlobalNamespace ? "" : (symbol.ContainingNamespace.ToString()+"."),
                target_name = targetname,
                target_namespace = symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToString(),
                target_simplename = symbol.Name,
                write_back = f_write_back,
                modulename = symbol.ContainingModule.Name,
                members = memberList.ToArray(),
            };
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            var refs = context.Compilation.ReferencedAssemblyNames.Select(n => n.Name).ToArray();
            if (!refs.Contains(ValidAssemblyName)) return;

            var receiver = context.SyntaxReceiver as OnChangeReceiver;
            var logs = "";
            //context.Compilation.SyntaxTrees
            //    .Select(tree => tree.GetCompilationUnitRoot().Usings.Any(u => u.Name.ToString() == "com.bbbirder"));
            try
            {

                var infoList = receiver.declarationList
                    .Select(d => GetDeclarationInfo(context, d))
                    .Where(d => d != null)
                    .OfType<DeclarationInfo>()
                    .Distinct()
                    .ToList();
                //.Where(a => IsExactlyCall(context, a))
                //.Select(a => GetInvocationInfo(context, a))
                //.Select(a => GetDeclarationInfo(context, a))
                //.Distinct()
                //.ToList();
                logs += context.Compilation.SourceModule.Name + " receives:\n";
                foreach (var info in infoList)
                {
                    logs += $"{info.src_name}\n";
                }
                logs += $"\nGenerations:\n";
                var template = Template.Parse(templ.classTemplate);
                foreach (var info in infoList)
                {
                    var targetFileName = $"{info.target_namespace.Replace('.', '_')}_{info.target_simplename}.g.cs";
                    var content = template.Render(info);
                    logs += targetFileName+"\n";
                    //context.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose);
                    context.AddSource(targetFileName, content);
                }

            }
            catch (Exception e)
            {

                try{
                    File.WriteAllText("./proxy.log", logs+"\n"+e.ToString());
                }
                catch { }
                throw;
            }

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new OnChangeReceiver());
        }
        public SemanticModel GetModel(GeneratorExecutionContext context, SyntaxNode node)
        {
            return context.Compilation.GetSemanticModel(node.SyntaxTree);
        }
    }
    public struct ClassInfo
    {
        public string name;
        public string nsname;
        public string fullname;
        public ClassDeclarationSyntax c;
    }
}
