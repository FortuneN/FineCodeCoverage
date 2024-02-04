using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IInitializable))]
    [Export(typeof(ICoverageTaggerProviderFactory))]
    internal class CoverageTaggerProviderFactory : ICoverageTaggerProviderFactory, IInitializable,IListener<NewCoverageLinesMessage>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILineSpanLogic lineSpanLogic;
        private IFileLineCoverage fileLineCoverage;

        [ImportingConstructor]
        public CoverageTaggerProviderFactory(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic
        )
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.AddListener(this);
            this.appOptionsProvider = appOptionsProvider;
            this.lineSpanLogic = lineSpanLogic;
        }
        public ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag
            where TCoverageTypeFilter : ICoverageTypeFilter, new()
        {
            return new CoverageTaggerProvider<TCoverageTypeFilter, TTag>(
                eventAggregator, appOptionsProvider, lineSpanLogic, tagger, fileLineCoverage
            );
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            fileLineCoverage = message.CoverageLines;
        }
    }

}
