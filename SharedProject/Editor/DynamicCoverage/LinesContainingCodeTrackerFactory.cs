using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ILinesContainingCodeTrackerFactory))]
    internal class LinesContainingCodeTrackerFactory : ILinesContainingCodeTrackerFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;
        private readonly ITrackingSpanRangeFactory trackingSpanRangeFactory;
        private readonly ITrackedCoverageLinesFactory trackedCoverageLinesFactory;
        private readonly ICoverageLineFactory coverageLineFactory;
        private readonly ITrackedContainingCodeTrackerFactory trackedContainingCodeTrackerFactory;

        [ImportingConstructor]
        public LinesContainingCodeTrackerFactory(
            ITrackingLineFactory trackingLineFactory,
            ITrackingSpanRangeFactory trackingSpanRangeFactory,
            ITrackedCoverageLinesFactory trackedCoverageLinesFactory,
            ICoverageLineFactory coverageLineFactory,
            ITrackedContainingCodeTrackerFactory trackedContainingCodeTrackerFactory)
        {
            this.trackingLineFactory = trackingLineFactory;
            this.trackingSpanRangeFactory = trackingSpanRangeFactory;
            this.trackedCoverageLinesFactory = trackedCoverageLinesFactory;
            this.coverageLineFactory = coverageLineFactory;
            this.trackedContainingCodeTrackerFactory = trackedContainingCodeTrackerFactory;
        }

        public IContainingCodeTracker Create(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange)
        {
            var trackingLineSpans = Enumerable.Range(containingRange.StartLine, containingRange.EndLine - containingRange.StartLine)
                .Select(lineNumber => trackingLineFactory.Create(textSnapshot, lineNumber)).ToList();
            var trackingSpanRange = trackingSpanRangeFactory.Create(trackingLineSpans);

            var coverageLines = GetTrackedCoverageLines(textSnapshot, lines);
            return trackedContainingCodeTrackerFactory.Create(trackingSpanRange, GetTrackedCoverageLines(textSnapshot,lines));
        }

        private ITrackedCoverageLines GetTrackedCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines)
        {
            var coverageLines = lines.Select(line => coverageLineFactory.Create(trackingLineFactory.Create(textSnapshot, line.Number - 1), line)).ToList();
            return trackedCoverageLinesFactory.Create(coverageLines.ToList());
        }

        public IContainingCodeTracker Create(ITextSnapshot textSnapshot, ILine line)
        {
            return trackedContainingCodeTrackerFactory.Create(GetTrackedCoverageLines(textSnapshot, new List<ILine> { line }));
        }
    }

}
