using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TrackedLines : ITrackedLines
    {
        private readonly List<IContainingCodeTracker> containingCodeTrackers;
        public TrackedLines(List<IContainingCodeTracker> containingCodeTrackers)
        {
            this.containingCodeTrackers = containingCodeTrackers;
        }


        // normalized spans
        public bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            var changed = false;
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var trackerChanged = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
                if (trackerChanged)
                {
                    changed = true;
                }
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
        }

    }

}
