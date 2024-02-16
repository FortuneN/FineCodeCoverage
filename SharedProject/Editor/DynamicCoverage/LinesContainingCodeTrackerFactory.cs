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

        public IContainingCodeTracker Create(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange,SpanTrackingMode spanTrackingMode)
        {
            var trackingLineSpans = Enumerable.Range(containingRange.StartLine, containingRange.EndLine - containingRange.StartLine + 1)
                .Select(lineNumber => trackingLineFactory.Create(textSnapshot, lineNumber, spanTrackingMode)).ToList();
            var trackingSpanRange = trackingSpanRangeFactory.Create(trackingLineSpans,textSnapshot);

            var coverageLines = GetTrackedCoverageLines(textSnapshot, lines, spanTrackingMode);
            return trackedContainingCodeTrackerFactory.Create(trackingSpanRange, coverageLines);
        }

        public IContainingCodeTracker Create(ITextSnapshot textSnapshot, ILine line, SpanTrackingMode spanTrackingMode)
        {
            return trackedContainingCodeTrackerFactory.Create(GetTrackedCoverageLines(textSnapshot, new List<ILine> { line },spanTrackingMode));
        }

        private ITrackedCoverageLines GetTrackedCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, SpanTrackingMode spanTrackingMode)
        {
            var coverageLines = lines.Select(line => coverageLineFactory.Create(
                trackingLineFactory.Create(textSnapshot, line.Number - 1,spanTrackingMode), line)
            ).ToList();
            return trackedCoverageLinesFactory.Create(coverageLines.ToList());
        }
    }
}
