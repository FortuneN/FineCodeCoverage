using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    class NewCodeTracker : INewCodeTracker
    {
        private readonly List<ITrackedNewCodeLine> trackedNewCodeLines = new List<ITrackedNewCodeLine>();
        private readonly bool isCSharp;
        private readonly ITrackedNewCodeLineFactory trackedNewCodeLineFactory;
        private readonly ILineExcluder codeLineExcluder;

        public NewCodeTracker(bool isCSharp, ITrackedNewCodeLineFactory trackedNewCodeLineFactory,ILineExcluder codeLineExcluder)
        {
            this.isCSharp = isCSharp;
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
        }

        public IEnumerable<IDynamicLine> Lines => trackedNewCodeLines.OrderBy(l => l.Line.Number).Select(l=>l.Line);

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
                // Take out the SpanTrackingMode ?
                var trackedNewCodeLine = trackedNewCodeLineFactory.Create(currentSnapshot, SpanTrackingMode.EdgeExclusive, lineNumber);
                var text = trackedNewCodeLine.GetText(currentSnapshot);
                if (!codeLineExcluder.ExcludeIfNotCode(text,isCSharp))
                {
                    trackedNewCodeLines.Add(trackedNewCodeLine);
                    requiresUpdate = true;
                }
            }
            return requiresUpdate;
        }
    }

}
