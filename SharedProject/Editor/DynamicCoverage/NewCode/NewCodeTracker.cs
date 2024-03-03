using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class NewCodeTracker : INewCodeTracker
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
            List<int> lineNumbers,
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

        private bool ProcessNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges, ITextSnapshot textSnapshot)
        {
            bool requiresChange = false;
            var startLineNumbers = newCodeCodeRanges.Select(newCodeCodeRange => newCodeCodeRange.StartLine).ToList();
            var removals = new List<ITrackedNewCodeLine>();
            foreach (ITrackedNewCodeLine trackedNewCodeLine in this.trackedNewCodeLines)
            {
                int trackedLineNumber = trackedNewCodeLine.Line.Number;
                bool removed = startLineNumbers.Remove(trackedLineNumber);
                if (!removed)
                {
                    requiresChange = true;
                    removals.Add(trackedNewCodeLine);
                }
            }

            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));

            foreach (int startLineNumber in startLineNumbers)
            {
                ITrackedNewCodeLine trackedNewCodeLine = this.CreateTrackedNewCodeLine(textSnapshot, startLineNumber);
                this.trackedNewCodeLines.Add(trackedNewCodeLine);
                requiresChange = true;
            }

            return requiresChange;
        }

        public bool ProcessChanges(
            ITextSnapshot currentSnapshot,
            List<SpanAndLineRange> potentialNewLines,
            IEnumerable<CodeSpanRange> newCodeCodeRanges
        ) => newCodeCodeRanges != null
                ? this.ProcessNewCodeCodeRanges(newCodeCodeRanges, currentSnapshot)
                : this.ProcessSpanAndLineRanges(potentialNewLines, currentSnapshot);

        private bool ProcessSpanAndLineRanges(List<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
        {
            bool requiresUpdate = false;
            var removals = new List<ITrackedNewCodeLine>();
            foreach (ITrackedNewCodeLine trackedNewCodeLine in this.trackedNewCodeLines)
            {
                TrackedNewCodeLineUpdate trackedNewCodeLineUpdate = trackedNewCodeLine.Update(currentSnapshot);

                potentialNewLines = potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != trackedNewCodeLineUpdate.LineNumber).ToList();

                if (this.codeLineExcluder.ExcludeIfNotCode(trackedNewCodeLineUpdate.Text, this.isCSharp))
                {
                    requiresUpdate = true;
                    removals.Add(trackedNewCodeLine);
                }
                else
                {
                    requiresUpdate = trackedNewCodeLineUpdate.LineUpdated;
                }
            };
            removals.ForEach(removal => this.trackedNewCodeLines.Remove(removal));

            IEnumerable<int> lineNumbers = this.GetLineNumbers(potentialNewLines);
            foreach (int lineNumber in lineNumbers)
            {
                requiresUpdate = this.AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber) || requiresUpdate;
            }

            return requiresUpdate;
        }

        private IEnumerable<int> GetLineNumbers(List<SpanAndLineRange> potentialNewLines)
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
