using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ILineSpanTagger<TTag> where TTag : ITag
    {
        TagSpan<TTag> GetTagSpan(ILineSpan lineSpan);
    }
}
