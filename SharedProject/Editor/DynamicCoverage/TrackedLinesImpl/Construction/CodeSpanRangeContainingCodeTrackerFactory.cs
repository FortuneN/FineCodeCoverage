using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ICodeSpanRangeContainingCodeTrackerFactory))]
    internal class CodeSpanRangeContainingCodeTrackerFactory : ICodeSpanRangeContainingCodeTrackerFactory
    {
        private readonly ITrackingLineFactory trackingLineFactory;
        private readonly ITrackingSpanRangeFactory trackingSpanRangeFactory;
        private readonly ITrackedCoverageLinesFactory trackedCoverageLinesFactory;
        private readonly ICoverageLineFactory coverageLineFactory;
        private readonly ITrackingSpanRangeContainingCodeTrackerFactory trackedContainingCodeTrackerFactory;
        private readonly INotIncludedLineFactory notIncludedLineFactory;

        [ImportingConstructor]
        public CodeSpanRangeContainingCodeTrackerFactory(
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

        public IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            var trackingSpanRange = CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode);
            var notIncludedLine = notIncludedLineFactory.Create(trackingSpanRange.GetFirstTrackingSpan(), textSnapshot);
            return trackedContainingCodeTrackerFactory.CreateNotIncluded(notIncludedLine, trackingSpanRange);
        }

        public IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            return trackedContainingCodeTrackerFactory.CreateCoverageLines(
                    CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode),
                    CreateTrackedCoverageLines(textSnapshot, lines, spanTrackingMode)
                   );
        }

        public IContainingCodeTracker CreateOtherLines(ITextSnapshot textSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            var trackingSpanRange = CreateTrackingSpanRange(textSnapshot, containingRange, spanTrackingMode);
            return trackedContainingCodeTrackerFactory.CreateOtherLines(trackingSpanRange);
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

        public IContainingCodeTracker CreateDirty(ITextSnapshot currentSnapshot, CodeSpanRange containingRange, SpanTrackingMode spanTrackingMode)
        {
            return trackedContainingCodeTrackerFactory.CreateDirty(CreateTrackingSpanRange(currentSnapshot, containingRange, spanTrackingMode), currentSnapshot);
        }
    }
}
