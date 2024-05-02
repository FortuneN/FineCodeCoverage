using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    internal interface IRazorGeneratedDocumentRootFinder
    {
        Task<SyntaxNode> FindSyntaxRootAsync(ITextBuffer textBuffer, string filePath);
    }
}
