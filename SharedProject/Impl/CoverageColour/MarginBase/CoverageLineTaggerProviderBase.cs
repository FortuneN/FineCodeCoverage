using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SharedProject.Core.Model;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal abstract class CoverageLineTaggerProviderBase<TTaggerListener, TTag> : ITaggerProvider, IListener<NewCoverageLinesMessage>
        where TTaggerListener : ITagger<TTag>, IListener<NewCoverageLinesMessage>
        where TTag : ITag
    {
        protected readonly IEventAggregator eventAggregator;
        private FileLineCoverage lastCoverageLines;

        public CoverageLineTaggerProviderBase(
            IEventAggregator eventAggregator
        )
        {
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            var tagger = CreateTagger(textBuffer, lastCoverageLines);
            eventAggregator.AddListener(tagger, false);
            return tagger as ITagger<T>;
        }

        protected abstract TTaggerListener CreateTagger(ITextBuffer textBuffer, FileLineCoverage lastCoverageLines);

        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
            NewCoverageLinesMessageReceived();
        }

        protected virtual void NewCoverageLinesMessageReceived()
        {

        }
    }
}
