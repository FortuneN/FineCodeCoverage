using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{

    [Export(typeof(ICoverageTaggerProviderFactory))]
    internal class CoverageTaggerProviderFactory : ICoverageTaggerProviderFactory
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILineSpanLogic lineSpanLogic;

        [ImportingConstructor]
        public CoverageTaggerProviderFactory(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic
        )
        {
            this.eventAggregator = eventAggregator;
            this.appOptionsProvider = appOptionsProvider;
            this.lineSpanLogic = lineSpanLogic;
        }
        public ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag
            where TCoverageTypeFilter : ICoverageTypeFilter, new()
        {
            return new CoverageTaggerProvider<TCoverageTypeFilter, TTag>(
                eventAggregator, appOptionsProvider, lineSpanLogic, tagger
            );
        }
    }

}
