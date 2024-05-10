using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Impl;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(IInitializable))]
    [Export(typeof(IDynamicCoverageManager))]
    internal class DynamicCoverageManager : IDynamicCoverageManager, IListener<NewCoverageLinesMessage>, IListener<TestExecutionStartingMessage>, IInitializable
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITrackedLinesFactory trackedLinesFactory;
        private readonly IBufferLineCoverageFactory bufferLineCoverageFactory;
        private readonly IDateTimeService dateTimeService;
        private LastCoverage lastCoverage;
        private DateTime lastTestExecutionStartingDate; 

        [ImportingConstructor]
        public DynamicCoverageManager(
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory,
            IBufferLineCoverageFactory bufferLineCoverageFactory,
            IDateTimeService dateTimeService)
        {
            this.bufferLineCoverageFactory = bufferLineCoverageFactory;
            this.dateTimeService = dateTimeService;
            _ = eventAggregator.AddListener(this);
            this.eventAggregator = eventAggregator;
            this.trackedLinesFactory = trackedLinesFactory;
        }
        public void Handle(NewCoverageLinesMessage message) => this.lastCoverage = new LastCoverage(message.CoverageLines, this.lastTestExecutionStartingDate);
        
        public void Handle(TestExecutionStartingMessage message) => this.lastTestExecutionStartingDate = this.dateTimeService.Now;
        
        public IBufferLineCoverage Manage(ITextInfo textInfo)
            => textInfo.TextBuffer.Properties.GetOrCreateSingletonProperty(
                () => this.bufferLineCoverageFactory.Create(this.lastCoverage, textInfo, this.eventAggregator, this.trackedLinesFactory)
            );
    }
}
