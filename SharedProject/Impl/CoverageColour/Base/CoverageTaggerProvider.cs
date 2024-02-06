using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
    internal class CoverageTaggerProvider<TCoverageTypeFilter, TTag> : ICoverageTaggerProvider<TTag>
         where TTag : ITag where TCoverageTypeFilter : ICoverageTypeFilter, new()
    {
        protected readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private readonly ILineSpanTagger<TTag> coverageTagger;
        private readonly IDynamicCoverageManager dynamicCoverageManager;
        private TCoverageTypeFilter coverageTypeFilter;

        public CoverageTaggerProvider(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> coverageTagger,
            IDynamicCoverageManager dynamicCoverageManager)
        {
            this.dynamicCoverageManager = dynamicCoverageManager;
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
            string filePath = null;
            if (textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                filePath = document.FilePath;
            }
            if (filePath == null)
            {
                return null;
            }
            var lastCoverageLines = dynamicCoverageManager.Manage(textView, textBuffer, filePath);
            return new CoverageTagger<TTag>(
                new TextBufferWithFilePath(textBuffer, filePath),
                lastCoverageLines, 
                coverageTypeFilter, 
                eventAggregator, 
                lineSpanLogic, 
                coverageTagger);
        }
    }

}
