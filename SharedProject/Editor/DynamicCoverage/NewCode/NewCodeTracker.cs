using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class NewCodeTracker : INewCodeTracker
    {
        private List<ITrackedNewCodeLine> trackedNewCodeLines = new List<ITrackedNewCodeLine>();
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
            foreach (var lineNumber in lineNumbers)
            {
                AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber);
            }
        }

        public IEnumerable<IDynamicLine> Lines => trackedNewCodeLines.OrderBy(l => l.Line.Number).Select(l=>l.Line);

        private bool ProcessNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges, ITextSnapshot textSnapshot)
        {
            var requiresChange = false;
            var startLineNumbers = newCodeCodeRanges.Select(newCodeCodeRange => newCodeCodeRange.StartLine).ToList();
            var removals = new List<ITrackedNewCodeLine>();
            foreach (var trackedNewCodeLine in trackedNewCodeLines)
            {
                var trackedLineNumber = trackedNewCodeLine.Line.Number;
                var removed = startLineNumbers.Remove(trackedLineNumber);
                if (!removed)
                {
                    requiresChange = true;
                    removals.Add(trackedNewCodeLine);
                }
            }
            removals.ForEach(removal => trackedNewCodeLines.Remove(removal));

            foreach (var startLineNumber in startLineNumbers)
            {
                var trackedNewCodeLine = CreateTrackedNewCodeLine(textSnapshot, startLineNumber);
                trackedNewCodeLines.Add(trackedNewCodeLine);
                requiresChange = true;
            }

            
            return requiresChange;

        }

        public bool ProcessChanges(
            ITextSnapshot currentSnapshot, 
            List<SpanAndLineRange> potentialNewLines, 
            IEnumerable<CodeSpanRange> newCodeCodeRanges)
        {
            if(newCodeCodeRanges != null)
            {
                return ProcessNewCodeCodeRanges(newCodeCodeRanges, currentSnapshot);
            }
            return ProcessSpanAndLineRanges(potentialNewLines, currentSnapshot);
        }

        private bool ProcessSpanAndLineRanges( List<SpanAndLineRange> potentialNewLines, ITextSnapshot currentSnapshot)
        {
            var requiresUpdate = false;
            var removals = new List<ITrackedNewCodeLine>();
            foreach (var trackedNewCodeLine in trackedNewCodeLines)
            {
                var trackedNewCodeLineUpdate = trackedNewCodeLine.Update(currentSnapshot);

                potentialNewLines = potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != trackedNewCodeLineUpdate.LineNumber).ToList();

                if (codeLineExcluder.ExcludeIfNotCode(trackedNewCodeLineUpdate.Text, isCSharp))
                {
                    requiresUpdate = true;
                    removals.Add(trackedNewCodeLine);
                }
                else
                {
                    requiresUpdate = trackedNewCodeLineUpdate.LineUpdated;
                }
            };
            removals.ForEach(removal => trackedNewCodeLines.Remove(removal));

            var lineNumbers = GetLineNumbers(potentialNewLines);
            foreach (var lineNumber in lineNumbers)
            {
                requiresUpdate = AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber) || requiresUpdate;
            }
            return requiresUpdate;
        }

        private IEnumerable<int> GetLineNumbers(List<SpanAndLineRange> potentialNewLines)
        {
            return potentialNewLines.Select(spanAndLineNumber => spanAndLineNumber.StartLineNumber).Distinct();
        }

        private bool AddTrackedNewCodeLineIfNotExcluded(ITextSnapshot currentSnapshot, int lineNumber)
        {
            var added = false;
            var trackedNewCodeLine = CreateTrackedNewCodeLine(currentSnapshot, lineNumber);
            var text = trackedNewCodeLine.GetText(currentSnapshot);
            if (!codeLineExcluder.ExcludeIfNotCode(text, isCSharp))
            {
                trackedNewCodeLines.Add(trackedNewCodeLine);
                added = true;
            }
            return added;
        }

        private ITrackedNewCodeLine CreateTrackedNewCodeLine(ITextSnapshot currentSnapshot, int lineNumber)
        {
            return trackedNewCodeLineFactory.Create(currentSnapshot, SpanTrackingMode.EdgeExclusive, lineNumber);
        }
    }

}
