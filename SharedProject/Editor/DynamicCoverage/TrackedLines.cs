using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedLines : ITrackedLines
    {
        private readonly List<IContainingCodeTracker> containingCodeTrackers;
        private readonly INewCodeTracker newCodeTracker;

        public TrackedLines(List<IContainingCodeTracker> containingCodeTrackers, INewCodeTracker newCodeTracker)
        {
            this.containingCodeTrackers = containingCodeTrackers;
            this.newCodeTracker = newCodeTracker;
        }


        // normalized spans
        public bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var spanAndLineRanges = newSpanChanges.Select(
                newSpanChange => new SpanAndLineRange(
                    newSpanChange, 
                    currentSnapshot.GetLineNumberFromPosition(newSpanChange.Start),
                    currentSnapshot.GetLineNumberFromPosition(newSpanChange.End)
                )).ToList();
            var changed = false;
            var removals = new List<IContainingCodeTracker>();
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var processResult = containingCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
                if (processResult.IsEmpty)
                {
                    removals.Add(containingCodeTracker);
                }
                spanAndLineRanges = processResult.UnprocessedSpans;
                if (processResult.Changed)
                {
                    changed = true;
                }
            }
            removals.ForEach(removal => containingCodeTrackers.Remove(removal));

            if (newCodeTracker != null)
            {
                var newCodeTrackerChanged = newCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
                changed = changed || newCodeTrackerChanged;
            }

            return changed;
        }

        public IEnumerable<IDynamicLine> GetAllLines()
        {
            return containingCodeTrackers.SelectMany(containingCodeTracker => containingCodeTracker.Lines)
                .Concat(newCodeTracker?.Lines ?? Enumerable.Empty<IDynamicLine>());
        }

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
            List<int> lineNumbers = new List<int>();
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var done = false;
                foreach (var line in containingCodeTracker.Lines)
                {
                    if(line.Number > endLineNumber)
                    {
                        done = true;
                        break;
                    }
                    if (line.Number >= startLineNumber)
                    {
                        lineNumbers.Add(line.Number);
                        yield return line;
                    }
                }
                if (done)
                {
                    break;
                }
            }
            var newLines = newCodeTracker?.Lines ?? Enumerable.Empty<IDynamicLine>();
            foreach (var line in newLines)
            {
                if (line.Number > endLineNumber)
                {
                    break;
                }
                if (line.Number >= startLineNumber)
                {
                    if(!lineNumbers.Contains(line.Number))
                    {
                        yield return line;
                    }
                }
            }
        }
        
    }

}
