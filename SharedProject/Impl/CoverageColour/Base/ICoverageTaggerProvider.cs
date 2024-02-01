using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageTaggerProvider<TTag> where TTag : ITag
    {
        ICoverageTagger<TTag> CreateTagger(ITextBuffer textBuffer);
    }
}
