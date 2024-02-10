using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Roslyn
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ITextSnapshotToSyntaxService))]
    class TextSnapshotToSyntaxService : ITextSnapshotToSyntaxService
    {
        public async Task<RootNodeAndLanguage> GetRootAndLanguageAsync(ITextSnapshot textSnapshot)
        {
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var language = document.Project.Language;
                var root = await document.GetSyntaxRootAsync();
                return new RootNodeAndLanguage(root, language);
            }
            return null;
        }
    }
}
