using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedLines : ITrackedLines
    {
        private readonly List<IContainingCodeTracker> containingCodeTrackers;
        private readonly INewCodeTracker newCodeTracker;
        private readonly IFileCodeSpanRangeService fileCodeSpanRangeService;

        public IReadOnlyList<IContainingCodeTracker> ContainingCodeTrackers => this.containingCodeTrackers;
        private readonly bool useFileCodeSpanRangeService;

        public TrackedLines(
            List<IContainingCodeTracker> containingCodeTrackers, 
            INewCodeTracker newCodeTracker, 
            IFileCodeSpanRangeService roslynService)
        {
            this.containingCodeTrackers = containingCodeTrackers;
            this.newCodeTracker = newCodeTracker;
            this.fileCodeSpanRangeService = roslynService;
            this.useFileCodeSpanRangeService = this.fileCodeSpanRangeService != null && newCodeTracker != null;
        }

        private List<SpanAndLineRange> GetSpanAndLineRanges(ITextSnapshot currentSnapshot, List<Span> newSpanChanges) 
            => newSpanChanges.Select(
                 newSpanChange => new SpanAndLineRange(
                     newSpanChange,
                     currentSnapshot.GetLineNumberFromPosition(newSpanChange.Start),
                     currentSnapshot.GetLineNumberFromPosition(newSpanChange.End)
                 )).ToList();

        private (bool, List<SpanAndLineRange>) ProcessContainingCodeTrackers(
            ITextSnapshot currentSnapshot, 
            List<SpanAndLineRange> spanAndLineRanges
            )
        {
            bool changed = false;
            var removals = new List<IContainingCodeTracker>();
            foreach (IContainingCodeTracker containingCodeTracker in this.containingCodeTrackers)
            {
                IContainingCodeTrackerProcessResult processResult = containingCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
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

            removals.ForEach(removal => this.containingCodeTrackers.Remove(removal));

            return (changed, spanAndLineRanges);
        }

        // normalized spans
        public bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            List<SpanAndLineRange> spanAndLineRanges = this.GetSpanAndLineRanges(currentSnapshot, newSpanChanges);
            (bool changed, List<SpanAndLineRange> unprocessedSpans) = this.ProcessContainingCodeTrackers(currentSnapshot, spanAndLineRanges);
            bool newCodeTrackerChanged = this.ProcessNewCodeTracker(currentSnapshot, unprocessedSpans);
            return changed || newCodeTrackerChanged;
        }

        private bool ProcessNewCodeTracker(ITextSnapshot currentSnapshot, List<SpanAndLineRange> spanAndLineRanges)
        {
            bool newCodeTrackerChanged = false;
            if (this.newCodeTracker != null)
            {
                List<CodeSpanRange> newCodeCodeRanges = this.useFileCodeSpanRangeService ? this.GetNewCodeCodeRanges(currentSnapshot, this.containingCodeTrackers.Select(ct => ct.GetState().CodeSpanRange).ToList()) : null;
                newCodeTrackerChanged = this.newCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges, newCodeCodeRanges);
            }

            return newCodeTrackerChanged;
        }

        private List<CodeSpanRange> GetNewCodeCodeRanges(
            ITextSnapshot currentSnapshot,
            List<CodeSpanRange> containingCodeTrackersCodeSpanRanges)
        {
            List<CodeSpanRange> fileCodeSpanRanges = this.fileCodeSpanRangeService.GetFileCodeSpanRanges(currentSnapshot);
            var newCodeCodeRanges = new List<CodeSpanRange>();
            int i = 0, j = 0;

            while (i < fileCodeSpanRanges.Count && j < containingCodeTrackersCodeSpanRanges.Count)
            {
                CodeSpanRange fileRange = fileCodeSpanRanges[i];
                CodeSpanRange trackerRange = containingCodeTrackersCodeSpanRanges[j];

                if (fileRange.EndLine < trackerRange.StartLine)
                {
                    // fileRange does not intersect with trackerRange, add it to the result
                    newCodeCodeRanges.Add(fileRange);
                    i++;
                }
                else if (fileRange.StartLine > trackerRange.EndLine)
                {
                    // fileRange is after trackerRange, move to the next trackerRange
                    j++;
                }
                else
                {
                    // roslynRange intersects with trackerRange, skip it
                    i++;
                }
            }

            // Add remaining fileCodeSpanRanges that come after the last trackerRange
            while (i < fileCodeSpanRanges.Count)
            {
                newCodeCodeRanges.Add(fileCodeSpanRanges[i]);
                i++;
            }

            return newCodeCodeRanges;
        }

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
            var lineNumbers = new List<int>();
            foreach (IContainingCodeTracker containingCodeTracker in this.containingCodeTrackers)
            {
                bool done = false;
                foreach (IDynamicLine line in containingCodeTracker.Lines)
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

            IEnumerable<IDynamicLine> newLines = this.newCodeTracker?.Lines ?? Enumerable.Empty<IDynamicLine>();
            foreach (IDynamicLine line in newLines)
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
