using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ICoverageTaggerProvider<TTag> where TTag : ITag
    {
        ICoverageTagger<TTag> CreateTagger(ITextView textView, ITextBuffer textBuffer);
    }
}
