using System.Collections.Immutable;
using System.Linq;
using BBBirder.UnityVue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityVue.SG
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BinderAttributeTargetUnusedSuppressor : DiagnosticSuppressor
    {
        private static readonly SuppressionDescriptor SuppressIDE0051 =
            new (
                id: "SUPPRESS_IDE0051",
                suppressedDiagnosticId: "IDE0051",
                justification: "Suppress IDE0051 for private members marked with BindableAttribute");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions
            => ImmutableArray.Create(SuppressIDE0051);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Id == "IDE0051")
                {
                    var tree = diagnostic.Location.SourceTree;
                    if (tree is null) continue;

                    var node = tree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
                    var symbol = context.GetSemanticModel(tree).GetDeclaredSymbol(node);
                    if (symbol != null)
                    {
                        bool hasBindableAttribute = symbol.GetAttributes()
                            .Any(attr => attr.AttributeClass.IsTypeOrSubTypeOf<BindableAttribute>());

                        if (hasBindableAttribute)
                        {
                            context.ReportSuppression(Suppression.Create(SuppressIDE0051, diagnostic));
                        }
                    }
                }
            }
        }
    }
}
