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
        private readonly ITrackingSpanRangeContainingCodeTrackerFactory trackedContainingCodeTrackerFactory;
        private readonly INotIncludedLineFactory notIncludedLineFactory;

        [ImportingConstructor]
        public LinesContainingCodeTrackerFactory(
            ITrackingLineFactory trackingLineFactory,
            ITrackingSpanRangeFactory trackingSpanRangeFactory,
            ITrackedCoverageLinesFactory trackedCoverageLinesFactory,
            ICoverageLineFactory coverageLineFactory,
            ITrackingSpanRangeContainingCodeTrackerFactory trackedContainingCodeTrackerFactory,
            INotIncludedLineFactory notIncludedLineFactory
            )
        {
            this.trackingLineFactory = trackingLineFactory;
            this.trackingSpanRangeFactory = trackingSpanRangeFactory;
            this.trackedCoverageLinesFactory = trackedCoverageLinesFactory;
            this.coverageLineFactory = coverageLineFactory;
            this.trackedContainingCodeTrackerFactory = trackedContainingCodeTrackerFactory;
            this.notIncludedLineFactory = notIncludedLineFactory;
        }

        public IContainingCodeTracker Create(
            ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange,SpanTrackingMode spanTrackingMode)
        {
            if(lines.Count > 0)
            {
                return CreateCoverageLines(textSnapshot, lines, containingRange, spanTrackingMode);
            }
            return CreateNotIncluded(textSnapshot, lines, containingRange, spanTrackingMode);
        }

        private IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            var trackingSpanRange = CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode);
            var notIncludedLine = notIncludedLineFactory.Create(trackingSpanRange.GetFirstTrackingSpan(), textSnapshot);
            return trackedContainingCodeTrackerFactory.CreateNotIncluded(notIncludedLine, trackingSpanRange);
        }

        private IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            return trackedContainingCodeTrackerFactory.CreateCoverageLines(
                    CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode),
                    CreateTrackedCoverageLines(textSnapshot, lines, spanTrackingMode)
                   );
        }

        public IContainingCodeTracker CreateOtherLinesTracker(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            var trackingSpanRange = CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode);
            return trackedContainingCodeTrackerFactory.CreateOtherLinesTracker(trackingSpanRange);
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
