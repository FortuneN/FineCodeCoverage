using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(IInitializable))]
    [Export(typeof(IDynamicCoverageManager))]
    internal class DynamicCoverageManager : IDynamicCoverageManager, IListener<NewCoverageLinesMessage>, IInitializable
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly IBufferLineCoverageFactory bufferLineCoverageFactory;
        private IFileLineCoverage lastCoverageLines;

        [ImportingConstructor]
        public DynamicCoverageManager(
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory,
            IBufferLineCoverageFactory bufferLineCoverageFactory)
        {
            this.bufferLineCoverageFactory = bufferLineCoverageFactory;
            eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
        }
        public void Handle(NewCoverageLinesMessage message)
        {
            lastCoverageLines = message.CoverageLines;
        }

        public IBufferLineCoverage Manage(ITextInfo textInfo)
        {
            return textInfo.TextBuffer.Properties.GetOrCreateSingletonProperty(
                () => bufferLineCoverageFactory.Create(lastCoverageLines, textInfo, eventAggregator, trackedLinesFactory)
            );
        }
    }
}
