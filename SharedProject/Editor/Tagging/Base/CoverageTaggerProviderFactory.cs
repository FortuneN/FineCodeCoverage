using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ICoverageTaggerProviderFactory))]
    internal class CoverageTaggerProviderFactory : ICoverageTaggerProviderFactory
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly IDynamicCoverageManager dynamicCoverageManager;
        private readonly ITextInfoFactory textInfoFactory;

        [ImportingConstructor]
        public CoverageTaggerProviderFactory(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            IDynamicCoverageManager dynamicCoverageManager,
            ITextInfoFactory textInfoFactory
        )
        {
            this.eventAggregator = eventAggregator;
            this.appOptionsProvider = appOptionsProvider;
            this.lineSpanLogic = lineSpanLogic;
            this.dynamicCoverageManager = dynamicCoverageManager;
            this.textInfoFactory = textInfoFactory;
        }
        public ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag
            where TCoverageTypeFilter : ICoverageTypeFilter, new()
                => new CoverageTaggerProvider<TCoverageTypeFilter, TTag>(
                    this.eventAggregator, this.appOptionsProvider, this.lineSpanLogic, tagger, this.dynamicCoverageManager, this.textInfoFactory
                );
    }
}
