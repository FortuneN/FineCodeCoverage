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
        private FileLineCoverage lastCoverageLines;
        private TCoverageTypeFilter coverageTypeFilter;

        public CoverageLineTaggerProviderBase(
            IEventAggregator eventAggregator,
            IAppOptionsProvider appOptionsProvider
        )
        {
            var appOptions = appOptionsProvider.Get();
            coverageTypeFilter =  new TCoverageTypeFilter() { AppOptions = appOptions };
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            var newCoverageTypeFilter = new TCoverageTypeFilter() { AppOptions = appOptions };
            if (newCoverageTypeFilter.Changed(coverageTypeFilter))
            {
                coverageTypeFilter = newCoverageTypeFilter;
                var message = new CoverageTypeFilterChangedMessage(newCoverageTypeFilter);
                eventAggregator.SendMessage(message);
            }
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            var tagger = CreateTagger(textBuffer, lastCoverageLines,eventAggregator,coverageTypeFilter);
            eventAggregator.AddListener(tagger);
            return tagger as ITagger<T>;
        }

        protected abstract TTaggerListener CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines, IEventAggregator eventAggregator,ICoverageTypeFilter coverageTypeFilter );

        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
        }

    }
}
