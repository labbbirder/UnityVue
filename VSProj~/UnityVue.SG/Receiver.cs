
using BBBirder.UnityVue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityVue.SG
{


    public class Receiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> ImplementInterfaceList { get; } = new();
        public List<TypeDeclarationSyntax> ExportFieldList { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax cds)
            {
                if (cds.BaseList != null && cds.BaseList.Types.Any(b => b.ToString().Contains(nameof(IDataProxy))))
                {
                    ImplementInterfaceList.Add(cds);
                }
                if (cds.AttributeLists.Count > 0 && cds.AttributeLists.SelectMany(a=>a.Attributes).Any(a=>a.ToString().Contains("ExportFields")))
                {
                    ExportFieldList.Add(cds);
                }
            }
        }
    }
}