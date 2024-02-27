using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using FineCodeCoverage.Editor.DynamicCoverage;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal class CoverageTaggerProvider<TCoverageTypeFilter, TTag> : ICoverageTaggerProvider<TTag>
         where TTag : ITag where TCoverageTypeFilter : ICoverageTypeFilter, new()
    {
        protected readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly ILineSpanTagger<TTag> coverageTagger;
        private readonly IDynamicCoverageManager dynamicCoverageManager;
        private readonly ITextInfoFactory textInfoFactory;
        private TCoverageTypeFilter coverageTypeFilter;

        public CoverageTaggerProvider(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> coverageTagger,
            IDynamicCoverageManager dynamicCoverageManager,
            ITextInfoFactory textInfoFactory)
        {
            this.dynamicCoverageManager = dynamicCoverageManager;
            this.textInfoFactory = textInfoFactory;
            var appOptions = appOptionsProvider.Get();
            coverageTypeFilter = CreateFilter(appOptions);
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
            this.coverageTagger = coverageTagger;
        }
        private TCoverageTypeFilter CreateFilter(IAppOptions appOptions)
        {
            var newCoverageTypeFilter = new TCoverageTypeFilter();
            newCoverageTypeFilter.Initialize(appOptions);
            return newCoverageTypeFilter;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            var newCoverageTypeFilter = CreateFilter(appOptions);
            if (newCoverageTypeFilter.Changed(coverageTypeFilter))
            {
                coverageTypeFilter = newCoverageTypeFilter;
                var message = new CoverageTypeFilterChangedMessage(newCoverageTypeFilter);
                eventAggregator.SendMessage(message);
            }
        }
        
        public ICoverageTagger<TTag> CreateTagger(ITextView textView, ITextBuffer textBuffer)
        {
            var textInfo = textInfoFactory.Create(textView, textBuffer);
            string filePath = textInfo.FilePath;
            if (filePath == null)
            {
                return null;
            }
            var lastCoverageLines = dynamicCoverageManager.Manage(textInfo);
            return new CoverageTagger<TTag>(
                textInfo,
                lastCoverageLines, 
                coverageTypeFilter, 
                eventAggregator, 
                lineSpanLogic, 
                coverageTagger);
        }
    }

}
