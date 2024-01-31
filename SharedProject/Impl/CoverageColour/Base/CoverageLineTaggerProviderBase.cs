using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;

namespace FineCodeCoverage.Impl
{
    internal abstract class CoverageLineTaggerProviderBase<TTaggerListener, TTag, TCoverageTypeFilter> : ITaggerProvider, IListener<NewCoverageLinesMessage>
        where TTaggerListener : ITagger<TTag>, IListener<NewCoverageLinesMessage>, IListener<CoverageTypeFilterChangedMessage>, IDisposable
        where TTag : ITag
        where TCoverageTypeFilter : ICoverageTypeFilter, new()
    {
        protected readonly IEventAggregator eventAggregator;
        private readonly ILineSpanLogic lineSpanLogic;
        private IFileLineCoverage lastCoverageLines;
        private TCoverageTypeFilter coverageTypeFilter;

        public CoverageLineTaggerProviderBase(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider,
            ILineSpanLogic lineSpanLogic
        )
        {
            var appOptions = appOptionsProvider.Get();
            coverageTypeFilter = CreateFilter(appOptions);
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.lineSpanLogic = lineSpanLogic;
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

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            var tagger = CreateCoverageTagger(textBuffer, lastCoverageLines,eventAggregator,coverageTypeFilter,lineSpanLogic);
            eventAggregator.AddListener(tagger);
            return tagger as ITagger<T>;
        }

        protected abstract TTaggerListener CreateCoverageTagger(
            ITextBuffer textBuffer, IFileLineCoverage lastCoverageLines, IEventAggregator eventAggregator,TCoverageTypeFilter coverageTypeFilter,ILineSpanLogic lineSpanLogic );

        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
        }

    }
}
