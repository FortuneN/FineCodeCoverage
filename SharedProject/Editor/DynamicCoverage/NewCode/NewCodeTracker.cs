using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class NewCodeTracker : INewCodeTracker
    {
        private readonly List<ITrackedNewCodeLine> trackedNewCodeLines = new List<ITrackedNewCodeLine>();
        private readonly bool isCSharp;
        private readonly ITrackedNewCodeLineFactory trackedNewCodeLineFactory;
        private readonly ILineExcluder codeLineExcluder;

        public NewCodeTracker(bool isCSharp, ITrackedNewCodeLineFactory trackedNewCodeLineFactory, ILineExcluder codeLineExcluder)
        {
            this.isCSharp = isCSharp;
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
        }

        public NewCodeTracker(
            bool isCSharp,
            ITrackedNewCodeLineFactory trackedNewCodeLineFactory,
            ILineExcluder codeLineExcluder,
            IEnumerable<int> lineNumbers,
            ITextSnapshot currentSnapshot
            )
        {
            this.isCSharp = isCSharp;
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
            foreach (int lineNumber in lineNumbers)
            {
                _ = this.AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber);
            }
        }

        public IEnumerable<IDynamicLine> Lines => this.trackedNewCodeLines.OrderBy(l => l.Line.Number).Select(l => l.Line);

        public IEnumerable<int> GetChangedLineNumbers(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines,
            IEnumerable<CodeSpanRange> newCodeCodeRanges
        ) => newCodeCodeRanges != null
                ? this.ProcessNewCodeCodeRanges(newCodeCodeRanges, currentSnapshot)
                : this.ProcessSpanAndLineRanges(potentialNewLines, currentSnapshot);

        #region NewCodeCodeRanges

        private List<int> ProcessNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges, ITextSnapshot textSnapshot)
        {
            var startLineNumbers = newCodeCodeRanges.Select(newCodeCodeRange => newCodeCodeRange.StartLine).ToList();
            IEnumerable<int> removed = this.RemoveAndReduceByLineNumbers(startLineNumbers);
            this.CreateTrackedNewCodeLines(startLineNumbers, textSnapshot);
            return removed.Concat(startLineNumbers).ToList();
        }

        private IEnumerable<int> RemoveAndReduceByLineNumbers(List<int> startLineNumbers)
        {
            var removals = this.trackedNewCodeLines.Where(
                trackedNewCodeLine => !startLineNumbers.Remove(trackedNewCodeLine.Line.Number)).ToList();

            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));
            return removals.Select(removal => removal.Line.Number);
        }

        private void CreateTrackedNewCodeLines(IEnumerable<int> lineNumbers, ITextSnapshot currentSnapshot)
        {
            foreach (int lineNumber in lineNumbers)
            {
                ITrackedNewCodeLine trackedNewCodeLine = this.CreateTrackedNewCodeLine(currentSnapshot, lineNumber);
                this.trackedNewCodeLines.Add(trackedNewCodeLine);
            }
        }
        #endregion
        #region SpanAndLineRanges
        private List<int> ProcessSpanAndLineRanges(List<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
        {
            (IEnumerable<int> updatedLineNumbers, List<SpanAndLineRange> updatedPotentialNewLines) =
                this.UpdateAndReduceBySpanAndLineRanges(currentSnapshot, potentialNewLines);
            IEnumerable<int> addedLineNumbers = this.AddTrackedNewCodeLinesIfNotExcluded(updatedPotentialNewLines, currentSnapshot);
            return updatedLineNumbers.Concat(addedLineNumbers).ToList();
        }

        private (IEnumerable<int> updatedLinesNumbers, List<SpanAndLineRange> potentialNewLines) UpdateAndReduceBySpanAndLineRanges(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines
        )
        {
            var updatedLineNumbers = new List<int>();
            var removals = new List<ITrackedNewCodeLine>();
            foreach (ITrackedNewCodeLine trackedNewCodeLine in this.trackedNewCodeLines)
            {
                (List<int> lineNumberUpdates, List<SpanAndLineRange> reducedPotentialNewLines) = this.UpdateAndReduce(
                    trackedNewCodeLine, currentSnapshot, potentialNewLines, removals);
                updatedLineNumbers.AddRange(lineNumberUpdates);
                potentialNewLines = reducedPotentialNewLines;
            };
            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));
            return (updatedLineNumbers.Distinct().ToList(), potentialNewLines);
        }

        private (List<int> lineNumberUpdates, List<SpanAndLineRange> reducedPotentialNewLines) UpdateAndReduce(
            ITrackedNewCodeLine trackedNewCodeLine,
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines,
            List<ITrackedNewCodeLine> removals
        )
        {
            var lineNumberUpdates = new List<int>();
            TrackedNewCodeLineUpdate trackedNewCodeLineUpdate = trackedNewCodeLine.Update(currentSnapshot);

            List<SpanAndLineRange> reducedPotentialNewLines = this.ReducePotentialNewLines(potentialNewLines, trackedNewCodeLineUpdate.NewLineNumber);

            bool excluded = this.RemoveTrackedNewCodeLineIfExcluded(removals, trackedNewCodeLine, trackedNewCodeLineUpdate.Text);
            if (excluded)
            {
                lineNumberUpdates.Add(trackedNewCodeLineUpdate.OldLineNumber);
            }
            else
            {
                if (trackedNewCodeLineUpdate.NewLineNumber != trackedNewCodeLineUpdate.OldLineNumber)
                {
                    lineNumberUpdates.Add(trackedNewCodeLineUpdate.OldLineNumber);
                    lineNumberUpdates.Add(trackedNewCodeLineUpdate.NewLineNumber);
                }
            }

            return (lineNumberUpdates, reducedPotentialNewLines);
        }

        private List<SpanAndLineRange> ReducePotentialNewLines(List<SpanAndLineRange> potentialNewLines, int updatedLineNumber)
            => potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != updatedLineNumber).ToList();

        private bool RemoveTrackedNewCodeLineIfExcluded(
            List<ITrackedNewCodeLine> removals,
            ITrackedNewCodeLine trackedNewCodeLine,
            string newText)
        {
            bool excluded = false;
            if (this.codeLineExcluder.ExcludeIfNotCode(newText, this.isCSharp))
            {
                excluded = true;
                removals.Add(trackedNewCodeLine);
            }

            return excluded;
        }
        private IEnumerable<int> AddTrackedNewCodeLinesIfNotExcluded(IEnumerable<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
            => this.GetDistinctStartLineNumbers(potentialNewLines)
                    .Where(lineNumber => this.AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber));

        private IEnumerable<int> GetDistinctStartLineNumbers(IEnumerable<SpanAndLineRange> potentialNewLines)
            => potentialNewLines.Select(spanAndLineNumber => spanAndLineNumber.StartLineNumber).Distinct();

        #endregion

        private bool AddTrackedNewCodeLineIfNotExcluded(ITextSnapshot currentSnapshot, int lineNumber)
        {
            bool added = false;
            ITrackedNewCodeLine trackedNewCodeLine = this.CreateTrackedNewCodeLine(currentSnapshot, lineNumber);
            string text = trackedNewCodeLine.GetText(currentSnapshot);
            if (!this.codeLineExcluder.ExcludeIfNotCode(text, this.isCSharp))
            {
                this.trackedNewCodeLines.Add(trackedNewCodeLine);
                added = true;
            }

            return added;
        }

        private ITrackedNewCodeLine CreateTrackedNewCodeLine(ITextSnapshot currentSnapshot, int lineNumber)
            => this.trackedNewCodeLineFactory.Create(currentSnapshot, SpanTrackingMode.EdgeExclusive, lineNumber);
    }
}
