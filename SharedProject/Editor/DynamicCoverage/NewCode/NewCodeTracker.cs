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
            List<CodeSpanRange> codeSpanRanges,
            ITextSnapshot currentSnapshot
            )
        {
            this.isCSharp = isCSharp;
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
            foreach (var codeSpanRange in codeSpanRanges)
            {
                var trackedNewCodeLine = CreateTrackedNewCodeLine(currentSnapshot, codeSpanRange.StartLine);
                trackedNewCodeLines.Add(trackedNewCodeLine);
            }
        }

        public IEnumerable<IDynamicLine> Lines => trackedNewCodeLines.OrderBy(l => l.Line.Number).Select(l=>l.Line);

        public bool ApplyNewCodeCodeRanges(IEnumerable<CodeSpanRange> newCodeCodeRanges)
        {
            var numBeforeFilter = trackedNewCodeLines.Count;
            ReduceLinesToCodeRangeStart(newCodeCodeRanges);
            return numBeforeFilter != trackedNewCodeLines.Count;
        }

        private void ReduceLinesToCodeRangeStart(IEnumerable<CodeSpanRange> newCodeCodeRanges)
        {
            trackedNewCodeLines = trackedNewCodeLines.Where(l => newCodeCodeRanges.Any(r => r.StartLine == l.Line.Number)).ToList();
        }

        public bool ProcessChanges(ITextSnapshot currentSnapshot, List<SpanAndLineRange> potentialNewLines)
        {
            var requiresUpdate = false;
            var removals = new List<ITrackedNewCodeLine>();
            foreach (var trackedNewCodeLine in  trackedNewCodeLines)
            {
                var trackedNewCodeLineUpdate = trackedNewCodeLine.Update(currentSnapshot);

                potentialNewLines = potentialNewLines.Where(spanAndLineRange => spanAndLineRange.StartLineNumber != trackedNewCodeLineUpdate.LineNumber).ToList();
                
                if (codeLineExcluder.ExcludeIfNotCode(trackedNewCodeLineUpdate.Text,isCSharp))
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

            var groupedByLineNumber = potentialNewLines.GroupBy(spanAndLineNumber => spanAndLineNumber.StartLineNumber);
            foreach (var grouping in groupedByLineNumber)
            {
                var lineNumber = grouping.Key;
                requiresUpdate = AddTrackedNewCodeLineIfNotExcluded(currentSnapshot, lineNumber) || requiresUpdate;
            }
            return requiresUpdate;
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
