using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedNewLineFactory
    {

    }

    

    class NewCodeTracker : INewCodeTracker
    {
        private readonly List<TrackedNewCodeLine> trackedNewCodeLines = new List<TrackedNewCodeLine>();
        private readonly bool isCSharp;

        public NewCodeTracker(bool isCSharp)
        {
            this.isCSharp = isCSharp;
        }

        public IEnumerable<IDynamicLine> Lines => trackedNewCodeLines.OrderBy(l => l.Number);

        public bool ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> potentialNewLines)
        {
            var requiresUpdate = false;
            var removals = new List<TrackedNewCodeLine>();
            foreach (var trackedNewCodeLine in  trackedNewCodeLines)
            {
                var newSnapshotSpan = trackedNewCodeLine.TrackingSpan.GetSpan(currentSnapshot);
                var line = currentSnapshot.GetLineFromPosition(newSnapshotSpan.End); // these two lines can go on the interface

                var lineNumber = line.LineNumber;

                potentialNewLines = potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != lineNumber).ToList();
                
                if (CodeLineExcluder.ExcludeIfNotCode(line.Extent,isCSharp))
                {
                    requiresUpdate = true;
                    removals.Add(trackedNewCodeLine);
                }
                else
                {
                    
                    if (lineNumber != trackedNewCodeLine.ActualLineNumber)
                    {
                        trackedNewCodeLine.ActualLineNumber = lineNumber;
                        requiresUpdate = true;
                    }
                }
            };
            removals.ForEach(removal => trackedNewCodeLines.Remove(removal));

            var groupedByLineNumber = potentialNewLines.GroupBy(spanAndLineNumber => spanAndLineNumber.StartLineNumber);
            foreach (var grouping in groupedByLineNumber)
            {
                var lineNumber = grouping.Key;
                var lineSpan = currentSnapshot.GetLineFromLineNumber(lineNumber).Extent;
                if (!CodeLineExcluder.ExcludeIfNotCode(lineSpan,isCSharp))
                {
                    var trackingSpan = currentSnapshot.CreateTrackingSpan(lineSpan, SpanTrackingMode.EdgeExclusive);
                    trackedNewCodeLines.Add(new TrackedNewCodeLine(lineNumber, trackingSpan));
                    requiresUpdate = true;
                }
            }
            return requiresUpdate;
        }
    }

}
