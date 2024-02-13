using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class NewCodeTracker : INewCodeTracker
    {
        private readonly List<TrackedNewCodeLine> trackedNewCodeLines = new List<TrackedNewCodeLine>();
        private class SpanAndLineNumber
        {
            public SpanAndLineNumber(Span span, int lineNumber)
            {
                Span = span;
                LineNumber = lineNumber;
            }

            public Span Span { get; }
            public int LineNumber { get; }
        }

        public IEnumerable<IDynamicLine> Lines => trackedNewCodeLines;

        public bool ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var requiresUpdate = false;
            var removals = new List<TrackedNewCodeLine>();

            trackedNewCodeLines.ForEach(trackedNewCodeLine =>
            {
                var newSnapshotSpan = trackedNewCodeLine.TrackingSpan.GetSpan(currentSnapshot);
                newSpanChanges = newSpanChanges.Where(newSpanChange => !newSpanChange.IntersectsWith(newSnapshotSpan)).ToList();
                if (newSnapshotSpan.IsEmpty || IgnoreLine(newSnapshotSpan))
                {
                    requiresUpdate = true;
                    removals.Add(trackedNewCodeLine);
                }
                else
                {
                    var newLineNumber = currentSnapshot.GetLineNumberFromPosition(newSnapshotSpan.Start);
                    if (newLineNumber != trackedNewCodeLine.ActualLineNumber)
                    {
                        trackedNewCodeLine.ActualLineNumber = newLineNumber;
                        requiresUpdate = true;
                    }
                }
            });
            removals.ForEach(removal => trackedNewCodeLines.Remove(removal));

            var groupedByLineNumber = newSpanChanges.Select(newSpanChange => new SpanAndLineNumber(newSpanChange, currentSnapshot.GetLineNumberFromPosition(newSpanChange.Start))).GroupBy(spanAndLineNumber => spanAndLineNumber.LineNumber);
            foreach (var grouping in groupedByLineNumber)
            {
                var lineNumber = grouping.Key;
                var lineSpan = currentSnapshot.GetLineFromLineNumber(lineNumber).Extent;
                if (!IgnoreLine(lineSpan))
                {
                    // to check - consistent with other usages
                    var trackingSpan = currentSnapshot.CreateTrackingSpan(lineSpan, SpanTrackingMode.EdgeInclusive);
                    trackedNewCodeLines.Add(new TrackedNewCodeLine(lineNumber, trackingSpan));
                    requiresUpdate = true;
                }

                // there is definitely common code with CoverageLine
            }
            return requiresUpdate;
        }

        // todo and does not start with single comment - need language
        private bool IgnoreLine(SnapshotSpan lineSpan)
        {
            var lineText = lineSpan.GetText();
            return lineText.Trim().Length == 0;
        }
    }

}
