using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.IndicatorVisibility;
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
        private readonly IFileExcluder[] fileExcluders;
        private readonly IFileIndicatorVisibility fileIndicatorVisibility;
        private TCoverageTypeFilter coverageTypeFilter;

        public CoverageTaggerProvider(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic,
            ILineSpanTagger<TTag> coverageTagger,
            IDynamicCoverageManager dynamicCoverageManager,
            ITextInfoFactory textInfoFactory,
            IFileExcluder[] fileExcluders,
            IFileIndicatorVisibility fileIndicatorVisibility)
        {
            this.dynamicCoverageManager = dynamicCoverageManager;
            this.textInfoFactory = textInfoFactory;
            this.fileExcluders = fileExcluders;
            this.fileIndicatorVisibility = fileIndicatorVisibility;
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

        private bool ExcludeContentTypeFile(string contentType,string filePath)
        {
            IFileExcluder contentTypeExcluder = this.fileExcluders.FirstOrDefault(fileExcluder => fileExcluder.ContentTypeName == contentType);
            return contentTypeExcluder != null && contentTypeExcluder.Exclude(filePath);
        }

        public ICoverageTagger<TTag> CreateTagger(ITextView textView, ITextBuffer textBuffer)
        {
            ITextInfo textInfo = this.textInfoFactory.Create(textView, textBuffer);
            string filePath = textInfo.FilePath;
            if (filePath == null || this.ExcludeContentTypeFile(textBuffer.ContentType.TypeName, filePath))
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
                this.coverageTagger,
                this.fileIndicatorVisibility
                );
        }
    }
}
