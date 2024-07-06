
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
        public List<TypeDeclarationSyntax> DeclarationList { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax cds)
            {
                if (cds.BaseList != null && cds.BaseList.Types.Any(b => b.ToString().Contains(nameof(IDataProxy))))
                {
                    DeclarationList.Add(cds);
                }
            }
        }
    }
}