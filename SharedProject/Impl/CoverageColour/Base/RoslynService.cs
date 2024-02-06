using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IRoslynService))]
    internal class RoslynService : IRoslynService
    {
        public async Task<List<ContainingCodeLineRange>> GetContainingCodeLineRangesAsync(ITextSnapshot textSnapshot, List<int> list)
        {
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var root = await document.GetSyntaxRootAsync();
                return root.DescendantNodes().Where(node =>
                    node.IsKind(SyntaxKind.MethodDeclaration) ||
                    node.IsKind(SyntaxKind.ConstructorDeclaration) ||
                    node.IsKind(SyntaxKind.AddAccessorDeclaration) ||
                    node.IsKind(SyntaxKind.RemoveAccessorDeclaration) ||
                    node.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                    node.IsKind(SyntaxKind.SetAccessorDeclaration)
                ).Select(declaration =>
                {
                    var span = declaration.Span.ToSpan();
                    var startLine = textSnapshot.GetLineFromPosition(span.Start);
                    var endLine = textSnapshot.GetLineFromPosition(span.End);
                    return new ContainingCodeLineRange
                    {
                        StartLine = startLine.LineNumber,
                        EndLine = endLine.LineNumber
                    };
                }).Where(containingCode => containingCode.ContainsAny(list)).ToList();
            }
            return new List<ContainingCodeLineRange>();
        }
    }
}
