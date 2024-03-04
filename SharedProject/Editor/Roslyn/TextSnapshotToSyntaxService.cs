using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITextSnapshotToSyntaxService))]
    internal class TextSnapshotToSyntaxService : ITextSnapshotToSyntaxService
    {
        public async Task<RootNodeAndLanguage> GetRootAndLanguageAsync(ITextSnapshot textSnapshot)
        {
            Microsoft.CodeAnalysis.Document document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                string language = document.Project.Language;
                Microsoft.CodeAnalysis.SyntaxNode root = await document.GetSyntaxRootAsync();
                return new RootNodeAndLanguage(root, language);
            }

            return null;
        }
    }
}
