using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    internal class ContainingCodeTracker
    {
        private List<ITrackingSpan> trackingSpans = new List<ITrackingSpan>();
        private List<TrackedLine> trackedLines = new List<TrackedLine>();
        public List<TrackedLine> TrackedLines => trackedLines;
        public void AddTrackedLine(TrackedLine trackedLine)
        {
            trackedLines.Add(trackedLine);
            AddTrackingSpan(trackedLine.TrackingSpan);
        }
        public void AddTrackingSpan(ITrackingSpan trackingSpan)
        {
            trackingSpans.Add(trackingSpan);
        }

        private bool ProcessTrackingSpanChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var containingCodeChanged = false;
            foreach (var trackingSpan in trackingSpans)
            {
                var currentSpan = trackingSpan.GetSpan(currentSnapshot).Span;
                var spanIntersected = newSpanChanges.Any(newSpan => newSpan.IntersectsWith(currentSpan));
                if (spanIntersected)
                {
                    containingCodeChanged = true;
                    trackedLines.Clear();
                    break;
                }
            }
            return containingCodeChanged;
        }

        private bool ProcessCoverageLineChanges(ITextSnapshot currentSnapshot)
        {
            var hasChanged = false;
            var removals = new List<TrackedLine>();
            foreach (var trackedLine in trackedLines)
            {
                var newSnapshotSpan = trackedLine.TrackingSpan.GetSpan(currentSnapshot);
                if (newSnapshotSpan.IsEmpty)
                {
                    hasChanged = true;
                    removals.Add(trackedLine);
                }
                else
                {
                    var newLineNumber = currentSnapshot.GetLineNumberFromPosition(newSnapshotSpan.Start) + 1;
                    if (newLineNumber != trackedLine.Line.Number)
                    {
                        hasChanged = true;
                    }
                    trackedLine.Line.Number = newLineNumber;
                }

            }
            removals.ForEach(r => trackedLines.Remove(r));
            return hasChanged;
        }

        public ContainingCodeChangeResult ProcessChanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            if (ProcessTrackingSpanChanges(currentSnapshot, newSpanChanges))
            {
                return ContainingCodeChangeResult.ContainingCodeChanged;
            }
            return ProcessCoverageLineChanges(currentSnapshot) ? ContainingCodeChangeResult.LineChanges : ContainingCodeChangeResult.Unchanged;
        }
    }

}
