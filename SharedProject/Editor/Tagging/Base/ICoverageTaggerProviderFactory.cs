using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ICoverageTaggerProviderFactory
    {
        ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag where TCoverageTypeFilter : ICoverageTypeFilter, new();
    }
}
