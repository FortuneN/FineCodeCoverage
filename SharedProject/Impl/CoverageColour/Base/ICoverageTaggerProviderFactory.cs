using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{

    internal interface ICoverageTaggerProviderFactory
    {
        ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag where TCoverageTypeFilter : ICoverageTypeFilter, new();
    }

}
