using System.Collections.Generic;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text;

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

        private (IEnumerable<int>, List<SpanAndLineRange>) ProcessContainingCodeTrackers(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> spanAndLineRanges
            )
        {
            var removals = new List<IContainingCodeTracker>();
            var allChangedLines = new List<int>();
            foreach (IContainingCodeTracker containingCodeTracker in this.containingCodeTrackers)
            {
                (IEnumerable<int> changedLines, List<SpanAndLineRange> unprocessedSpans) =
                    this.ProcessContainingCodeTracker(removals, containingCodeTracker, currentSnapshot, spanAndLineRanges);
                allChangedLines.AddRange(changedLines);
                spanAndLineRanges = unprocessedSpans;
            }

            removals.ForEach(removal => this.containingCodeTrackers.Remove(removal));

            return (allChangedLines, spanAndLineRanges);
        }

        private (IEnumerable<int> changedLines, List<SpanAndLineRange> unprocessedSpans) ProcessContainingCodeTracker(
            List<IContainingCodeTracker> removals,
            IContainingCodeTracker containingCodeTracker,
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> spanAndLineRanges
        )
        {
            IContainingCodeTrackerProcessResult processResult = containingCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
            if (processResult.IsEmpty)
            {
                removals.Add(containingCodeTracker);
            }

            return (processResult.ChangedLines, processResult.UnprocessedSpans);
        }

        // normalized spans
        public IEnumerable<int> GetChangedLineNumbers(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            List<SpanAndLineRange> spanAndLineRanges = this.GetSpanAndLineRanges(currentSnapshot, newSpanChanges);
            (IEnumerable<int> changedLines, List<SpanAndLineRange> unprocessedSpans) =
                this.ProcessContainingCodeTrackers(currentSnapshot, spanAndLineRanges);
            IEnumerable<int> newCodeTrackerChangedLines = this.GetNewCodeTrackerChangedLineNumbers(currentSnapshot, unprocessedSpans);
            return changedLines.Concat(newCodeTrackerChangedLines).Distinct();
        }

        private IEnumerable<int> GetNewCodeTrackerChangedLineNumbers(ITextSnapshot currentSnapshot, List<SpanAndLineRange> spanAndLineRanges)
            => this.newCodeTracker == null ? Enumerable.Empty<int>() : this.GetNewCodeTrackerChangedLineNumbersActual(currentSnapshot, spanAndLineRanges);

        private IEnumerable<int> GetNewCodeTrackerChangedLineNumbersActual(ITextSnapshot currentSnapshot, List<SpanAndLineRange> spanAndLineRanges)
        {
            List<CodeSpanRange> newCodeCodeRanges = this.GetNewCodeCodeRanges(currentSnapshot);
            return this.newCodeTracker.GetChangedLineNumbers(currentSnapshot, spanAndLineRanges, newCodeCodeRanges);
        }

        private List<CodeSpanRange> GetNewCodeCodeRanges(ITextSnapshot currentSnapshot)
            => this.useFileCodeSpanRangeService ? this.GetNewCodeCodeRangesActual(currentSnapshot) : null;

        private List<CodeSpanRange> GetNewCodeCodeRangesActual(ITextSnapshot currentSnapshot)
            => this.GetNewCodeCodeRanges(currentSnapshot, this.GetContainingCodeTrackersCodeSpanRanges()).ToList();

        private List<CodeSpanRange> GetContainingCodeTrackersCodeSpanRanges()
            => this.containingCodeTrackers.Select(ct => ct.GetState().CodeSpanRange).ToList();

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
                    // fileRange intersects with trackerRange, skip it
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

        private (bool done, IEnumerable<IDynamicLine> lines) GetLines(IEnumerable<IDynamicLine> dynamicLines, int startLineNumber, int endLineNumber)
        {
            IEnumerable<IDynamicLine> linesApplicableToStartLineNumber = this.LinesApplicableToStartLineNumber(dynamicLines, startLineNumber);
            var lines = linesApplicableToStartLineNumber.TakeWhile(l => l.Number <= endLineNumber).ToList();
            bool done = lines.Count != linesApplicableToStartLineNumber.Count();
            return (done, lines);
        }

        private IEnumerable<IDynamicLine> LinesApplicableToStartLineNumber(IEnumerable<IDynamicLine> dynamicLines, int startLineNumber)
            => dynamicLines.Where(l => l.Number >= startLineNumber);

        private IEnumerable<IDynamicLine> GetLinesFromContainingCodeTrackers(int startLineNumber, int endLineNumber)
            => this.containingCodeTrackers.Select(containingCodeTracker => this.GetLines(containingCodeTracker.Lines, startLineNumber, endLineNumber))
                .TakeUntil(a => a.done).SelectMany(a => a.lines);

        private IEnumerable<IDynamicLine> NewCodeTrackerLines() => this.newCodeTracker?.Lines ?? Enumerable.Empty<IDynamicLine>();

        private IEnumerable<IDynamicLine> GetNewLines(int startLineNumber, int endLineNumber)
            => this.LinesApplicableToStartLineNumber(this.NewCodeTrackerLines(), startLineNumber)
                .TakeWhile(l => l.Number <= endLineNumber);

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
            => this.GetLinesFromContainingCodeTrackers(startLineNumber, endLineNumber)
                .Concat(this.GetNewLines(startLineNumber, endLineNumber))
                .Distinct(new DynamicLineByLineNumberComparer()).ToList();

        private class DynamicLineByLineNumberComparer : IEqualityComparer<IDynamicLine>
        {
            public bool Equals(IDynamicLine x, IDynamicLine y) => x.Number == y.Number;
            public int GetHashCode(IDynamicLine obj) => obj.Number;
        }
    }
}
