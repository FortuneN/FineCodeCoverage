using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

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
            IAppOptions appOptions = appOptionsProvider.Get();
            this.coverageTypeFilter = this.CreateFilter(appOptions);
            appOptionsProvider.OptionsChanged += this.AppOptionsProvider_OptionsChanged;
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
            TCoverageTypeFilter newCoverageTypeFilter = this.CreateFilter(appOptions);
            if (newCoverageTypeFilter.Changed(this.coverageTypeFilter))
            {
                this.coverageTypeFilter = newCoverageTypeFilter;
                var message = new CoverageTypeFilterChangedMessage(newCoverageTypeFilter);
                this.eventAggregator.SendMessage(message);
            }
        }

        public ICoverageTagger<TTag> CreateTagger(ITextView textView, ITextBuffer textBuffer)
        {
            ITextInfo textInfo = this.textInfoFactory.Create(textView, textBuffer);
            string filePath = textInfo.FilePath;
            if (filePath == null)
            {
                return null;
            }

            IBufferLineCoverage bufferLineCoverage = this.dynamicCoverageManager.Manage(textInfo);
            return new CoverageTagger<TTag>(
                textInfo,
                bufferLineCoverage,
                this.coverageTypeFilter,
                this.eventAggregator,
                this.lineSpanLogic,
                this.coverageTagger);
        }
    }
}
