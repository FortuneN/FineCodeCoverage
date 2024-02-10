using Microsoft.VisualStudio.Text;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Roslyn
{
    internal interface ITextSnapshotToSyntaxService
    {
        Task<RootNodeAndLanguage> GetRootAndLanguageAsync(ITextSnapshot textSnapshot);
    }
}
