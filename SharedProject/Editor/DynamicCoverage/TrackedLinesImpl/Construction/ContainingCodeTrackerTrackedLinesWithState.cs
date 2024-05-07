using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction
{
    [ExcludeFromCodeCoverage]
    internal class ContainingCodeTrackerTrackedLinesWithState : IContainingCodeTrackerTrackedLines
    {
        public IContainingCodeTrackerTrackedLines Wrapped { get; }
        public ContainingCodeTrackerTrackedLinesWithState(IContainingCodeTrackerTrackedLines trackedLines, bool usedFileCodeSpanRangeService)
        {
            this.Wrapped = trackedLines;
            this.UsedFileCodeSpanRangeService = usedFileCodeSpanRangeService;
        }

        public bool UsedFileCodeSpanRangeService { get; set; }
        public IReadOnlyList<IContainingCodeTracker> ContainingCodeTrackers => this.Wrapped.ContainingCodeTrackers;
        public INewCodeTracker NewCodeTracker => this.Wrapped.NewCodeTracker;

        public IEnumerable<int> GetChangedLineNumbers(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
            => this.Wrapped.GetChangedLineNumbers(currentSnapshot, newSpanChanges);
        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
            => this.Wrapped.GetLines(startLineNumber, endLineNumber);
    }
}
