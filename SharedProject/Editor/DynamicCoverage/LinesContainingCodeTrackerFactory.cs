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
            return trackedContainingCodeTrackerFactory.Create(
                CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode),
                CreateTrackedCoverageLines(textSnapshot, lines, spanTrackingMode)
               );
        }

        public IContainingCodeTracker Create(ITextSnapshot textSnapshot, ILine line, SpanTrackingMode spanTrackingMode)
        {
            return trackedContainingCodeTrackerFactory.Create(CreateTrackedCoverageLines(textSnapshot, new List<ILine> { line },spanTrackingMode));
        }

        private ITrackingSpanRange CreateTrackingSpanRange(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            var startTrackingSpan = trackingLineFactory.CreateTrackingSpan(textSnapshot, containingRange.StartLine, spanTrackingMode);
            var endTrackingSpan = trackingLineFactory.CreateTrackingSpan(textSnapshot, containingRange.EndLine, spanTrackingMode);
            return trackingSpanRangeFactory.Create(startTrackingSpan, endTrackingSpan, textSnapshot);
        }

        private ITrackedCoverageLines CreateTrackedCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, SpanTrackingMode spanTrackingMode)
        {
            var coverageLines = lines.Select(line => coverageLineFactory.Create(
                trackingLineFactory.CreateTrackingSpan(textSnapshot, line.Number - 1,spanTrackingMode), line)
            ).ToList();
            return trackedCoverageLinesFactory.Create(coverageLines.ToList());
        }
    }
}
