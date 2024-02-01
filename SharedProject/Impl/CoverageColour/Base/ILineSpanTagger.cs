using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
    interface ILineSpanTagger<TTag> where TTag : ITag
    {
        TagSpan<TTag> GetTagSpan(ILineSpan lineSpan);
    }

}
