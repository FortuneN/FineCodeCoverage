using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ITextSnapshotToSyntaxService
    {
        Task<RootNodeAndLanguage> GetRootAndLanguageAsync(ITextSnapshot textSnapshot);
    }
}
