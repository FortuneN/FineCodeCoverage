using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal class TrackedLines : ITrackedLines
    {
        private List<ContainingCodeTracker> containingCodeTrackers = new List<ContainingCodeTracker>();

        public TrackedLines(List<ILine> lines, ITextSnapshot textSnapshot, List<ContainingCodeLineRange> orderedContainingCodeLineRanges)
        {
            var lineIndex = 0;
            foreach (var containingCodeLineRange in orderedContainingCodeLineRanges)
            {
                var containingCodeTracker = new ContainingCodeTracker();

                for (var i = containingCodeLineRange.StartLine; i <= containingCodeLineRange.EndLine; i++)
                {
                    var span = textSnapshot.GetLineFromLineNumber(i).Extent;
                    var trackingSpan = textSnapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
                    var line = lines[lineIndex];
                    // instead of keep moving line numbers around****************************************
                    if (line.Number - 1 == i)
                    {
                        var trackedLine = new TrackedLine(line, trackingSpan);
                        containingCodeTracker.AddTrackedLine(trackedLine);
                        lineIndex++;
                    }
                    else
                    {
                        containingCodeTracker.AddTrackingSpan(trackingSpan);
                    }
                }
                containingCodeTrackers.Add(containingCodeTracker);
            }
        }

        public bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var changed = false;
            var removals = new List<ContainingCodeTracker>();
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var changeResult = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
                if (changeResult == ContainingCodeChangeResult.ContainingCodeChanged)
                {
                    changed = true;
                    removals.Add(containingCodeTracker);
                }
                else if (changeResult == ContainingCodeChangeResult.LineChanges)
                {
                    changed = true;
                }
            }
            removals.ForEach(r => containingCodeTrackers.Remove(r));
            return changed;
        }

        public IEnumerable<ILine> GetLines(int startLineNumber, int endLineNumber)
        {
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                // todo - no need to iterate over all
                foreach (var trackedLine in containingCodeTracker.TrackedLines)
                {
                    if (trackedLine.Line.Number >= startLineNumber && trackedLine.Line.Number <= endLineNumber)
                    {
                        yield return trackedLine.Line;
                    }
                }
            }
        }

    }

}
