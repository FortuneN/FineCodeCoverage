using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.IndicatorVisibility;
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
        private readonly IFileExcluder[] fileExcluders;
        private readonly IFileIndicatorVisibility fileIndicatorVisibility;

        [ImportingConstructor]
        public CoverageTaggerProviderFactory(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            IDynamicCoverageManager dynamicCoverageManager,
            ITextInfoFactory textInfoFactory,
            [ImportMany]
            IFileExcluder[] fileExcluders,
            IFileIndicatorVisibility fileIndicatorVisibility
        )
        {
            this.eventAggregator = eventAggregator;
            this.appOptionsProvider = appOptionsProvider;
            this.lineSpanLogic = lineSpanLogic;
            this.dynamicCoverageManager = dynamicCoverageManager;
            this.textInfoFactory = textInfoFactory;
            this.fileExcluders = fileExcluders;
            this.fileIndicatorVisibility = fileIndicatorVisibility;
        }
        public ICoverageTaggerProvider<TTag> Create<TTag, TCoverageTypeFilter>(ILineSpanTagger<TTag> tagger)
            where TTag : ITag
            where TCoverageTypeFilter : ICoverageTypeFilter, new()
                => new CoverageTaggerProvider<TCoverageTypeFilter, TTag>(
                    this.eventAggregator,
                    this.appOptionsProvider,
                    this.lineSpanLogic,
                    tagger,
                    this.dynamicCoverageManager,
                    this.textInfoFactory,
                    this.fileExcluders,
                    this.fileIndicatorVisibility
                );
    }
}
