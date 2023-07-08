using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MainCollectedNode = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
namespace com.bbbirder.onchange {


    public class OnChangeReceiver : ISyntaxReceiver {
        public List<TypeDeclarationSyntax> declarationList { get; } = new ();
        public string logs;
        public bool HasAttribute(TypeDeclarationSyntax cds) {
            return cds.AttributeLists.Count>0;
        }
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if(syntaxNode is ClassDeclarationSyntax cds) {
                if (HasAttribute(cds)) {
                    declarationList.Add(cds);
                }
            }
            else if(syntaxNode is RecordDeclarationSyntax rds)
            {
                if (HasAttribute(rds))
                {
                    declarationList.Add(rds);
                }
            }
        }
    }


}
