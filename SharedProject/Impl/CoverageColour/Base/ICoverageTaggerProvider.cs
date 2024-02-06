using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageTaggerProvider<TTag> where TTag : ITag
    {
        ICoverageTagger<TTag> CreateTagger(ITextView textView,ITextBuffer textBuffer);
    }
}
