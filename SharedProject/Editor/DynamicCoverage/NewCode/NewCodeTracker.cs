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

        private bool RemoveAndReduceByLineNumbers(List<int> startLineNumbers)
        {
            var removals = this.trackedNewCodeLines.Where(
                trackedNewCodeLine => !startLineNumbers.Remove(trackedNewCodeLine.Line.Number)).ToList();

            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));
            return removals.Count > 0;
        }
        private bool ProcessNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges, ITextSnapshot textSnapshot)
        {
            var startLineNumbers = newCodeCodeRanges.Select(newCodeCodeRange => newCodeCodeRange.StartLine).ToList();
            bool removed = this.RemoveAndReduceByLineNumbers(startLineNumbers);
            bool created = this.CreateTrackedNewCodeLines(startLineNumbers, textSnapshot);
            return removed || created;
        }

        private bool CreateTrackedNewCodeLines(IEnumerable<int> lineNumbers, ITextSnapshot currentSnapshot)
        {
            bool created = false;
            foreach (int lineNumber in lineNumbers)
            {
                ITrackedNewCodeLine trackedNewCodeLine = this.CreateTrackedNewCodeLine(currentSnapshot, lineNumber);
                this.trackedNewCodeLines.Add(trackedNewCodeLine);
                created = true;
            }

            return created;
        }

        public bool ProcessChanges(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines,
            IEnumerable<CodeSpanRange> newCodeCodeRanges
        ) => newCodeCodeRanges != null
                ? this.ProcessNewCodeCodeRanges(newCodeCodeRanges, currentSnapshot)
                : this.ProcessSpanAndLineRanges(potentialNewLines, currentSnapshot);

        private (bool, List<SpanAndLineRange>) UpdateAndReduceBySpanAndLineRanges(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines
        )
        {
            bool requiresUpdate = false;
            var removals = new List<ITrackedNewCodeLine>();
            foreach (ITrackedNewCodeLine trackedNewCodeLine in this.trackedNewCodeLines)
            {
                (bool needsUpdate, List<SpanAndLineRange> reducedPotentialNewLines) = this.UpdateAndReduce(
                    trackedNewCodeLine, currentSnapshot, potentialNewLines, removals);

                requiresUpdate = requiresUpdate || needsUpdate;
                potentialNewLines = reducedPotentialNewLines;
            };
            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));
            return (requiresUpdate, potentialNewLines);
        }

        private (bool requiresUpdate, List<SpanAndLineRange> reducedPotentialNewLines) UpdateAndReduce(
            ITrackedNewCodeLine trackedNewCodeLine,
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines,
            List<ITrackedNewCodeLine> removals
        )
        {
            TrackedNewCodeLineUpdate trackedNewCodeLineUpdate = trackedNewCodeLine.Update(currentSnapshot);

            List<SpanAndLineRange> reducedPotentialNewLines = this.ReducePotentialNewLines(potentialNewLines, trackedNewCodeLineUpdate.LineNumber);

            bool requiresUpdate = this.RemoveTrackedNewCodeLineIfExcluded(removals, trackedNewCodeLine, trackedNewCodeLineUpdate);

            return (requiresUpdate, reducedPotentialNewLines);
        }

        private bool RemoveTrackedNewCodeLineIfExcluded(
            List<ITrackedNewCodeLine> removals,
            ITrackedNewCodeLine trackedNewCodeLine,
            TrackedNewCodeLineUpdate trackedNewCodeLineUpdate)
        {
            bool requiresUpdate;
            if (this.codeLineExcluder.ExcludeIfNotCode(trackedNewCodeLineUpdate.Text, this.isCSharp))
            {
                requiresUpdate = true;
                removals.Add(trackedNewCodeLine);
            }
            else
            {
                requiresUpdate = trackedNewCodeLineUpdate.LineUpdated;
            }

            return requiresUpdate;
        }

        private List<SpanAndLineRange> ReducePotentialNewLines(List<SpanAndLineRange> potentialNewLines, int updatedLineNumber)
            => potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != updatedLineNumber).ToList();

        private bool ProcessSpanAndLineRanges(List<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
        {
            (bool requiresUpdate, List<SpanAndLineRange> updatedPotentialNewLines) = this.UpdateAndReduceBySpanAndLineRanges(currentSnapshot, potentialNewLines);
            bool added = this.AddTrackedNewCodeLinesIfNotExcluded(updatedPotentialNewLines, currentSnapshot);
            return requiresUpdate || added;
        }

        private bool AddTrackedNewCodeLinesIfNotExcluded(IEnumerable<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
            => this.GetDistinctStartLineNumbers(potentialNewLines)
                    .Any(lineNumber => this.AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber));

        private IEnumerable<int> GetDistinctStartLineNumbers(IEnumerable<SpanAndLineRange> potentialNewLines)
            => potentialNewLines.Select(spanAndLineNumber => spanAndLineNumber.StartLineNumber).Distinct();

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
