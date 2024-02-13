using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

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
            var changed = false;
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var processResult = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
                newSpanChanges = processResult.UnprocessedSpans;
                if (processResult.Changed)
                {
                    changed = true;
                }
                if(newSpanChanges.Count == 0)
                {
                    break;
                }
            }
            if(newSpanChanges.Count > 0)
            {
                changed = newCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
            }
            return changed;
        }

        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
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
                        yield return line;
                    }
                }
                if (done)
                {
                    break;
                }
            }
            foreach (var line in newCodeTracker.Lines)
            {
                if (line.Number > endLineNumber)
                {
                    break;
                }
                if (line.Number >= startLineNumber)
                {
                    yield return line;
                }
            }
        }

    }

}
