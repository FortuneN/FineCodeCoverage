using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IRolsynCodeSpanRangeService
    {
        List<CodeSpanRange> Get(ITextSnapshot snapshot);
    }

    [Export(typeof(IRolsynCodeSpanRangeService))]
    internal class RolsynCodeSpanRangeService : IRolsynCodeSpanRangeService
    {
        private readonly IThreadHelper threadHelper;
        private readonly IRoslynService roslynService;

        [ImportingConstructor]
        public RolsynCodeSpanRangeService(
            IThreadHelper threadHelper, 
            IRoslynService roslynService
        )
        {
            this.threadHelper = threadHelper;
            this.roslynService = roslynService;
        }
        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            var startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            var endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }

        public List<CodeSpanRange> Get(ITextSnapshot currentSnapshot)
        {
            var roslynContainingCodeSpans = threadHelper.JoinableTaskFactory.Run(() => roslynService.GetContainingCodeSpansAsync(currentSnapshot));
            return roslynContainingCodeSpans.Select(roslynCodeSpan => GetCodeSpanRange(roslynCodeSpan, currentSnapshot)).ToList();
        }
    }


    internal class TrackedLines : ITrackedLines
    {
        private readonly List<IContainingCodeTracker> containingCodeTrackers;
        private readonly INewCodeTracker newCodeTracker;
        private readonly IRolsynCodeSpanRangeService roslynCodeSpanRangeService;

        public IReadOnlyList<IContainingCodeTracker> ContainingCodeTrackers => containingCodeTrackers;

        public TrackedLines(
            List<IContainingCodeTracker> containingCodeTrackers, 
            INewCodeTracker newCodeTracker, 
            IRolsynCodeSpanRangeService roslynService)
        {
            this.containingCodeTrackers = containingCodeTrackers;
            this.newCodeTracker = newCodeTracker;
            this.roslynCodeSpanRangeService = roslynService;
        }


        // normalized spans
        public bool Changed(ITextSnapshot currentSnapshot, List<Span> newSpanChanges)
        {
            List<CodeSpanRange> containingCodeTrackersCodeSpanRanges = new List<CodeSpanRange>();
            List<CodeSpanRange> roslynCodeSpanRanges = null;
            if(roslynCodeSpanRangeService != null)
            {
                roslynCodeSpanRanges = roslynCodeSpanRangeService.Get(currentSnapshot);
            }
            var spanAndLineRanges = newSpanChanges.Select(
                newSpanChange => new SpanAndLineRange(
                    newSpanChange, 
                    currentSnapshot.GetLineNumberFromPosition(newSpanChange.Start),
                    currentSnapshot.GetLineNumberFromPosition(newSpanChange.End)
                )).ToList();
            var changed = false;
            var removals = new List<IContainingCodeTracker>();
            foreach (var containingCodeTracker in containingCodeTrackers)
            {
                var processResult = containingCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
                if (processResult.IsEmpty)
                {
                    removals.Add(containingCodeTracker);
                }
                else
                {
                    if(roslynCodeSpanRangeService != null)
                    {
                        containingCodeTrackersCodeSpanRanges.Add(containingCodeTracker.GetState().CodeSpanRange);
                    }
                }
                spanAndLineRanges = processResult.UnprocessedSpans;
                if (processResult.Changed)
                {
                    changed = true;
                }
            }
            removals.ForEach(removal => containingCodeTrackers.Remove(removal));

            if (newCodeTracker != null)
            {
                var newCodeTrackerChanged = newCodeTracker.ProcessChanges(currentSnapshot, spanAndLineRanges);
                if (roslynCodeSpanRangeService != null)
                {
                    var newCodeCodeRanges = GetNewCodeCodeRanges(roslynCodeSpanRanges,containingCodeTrackersCodeSpanRanges);
                    var requiresChange = newCodeTracker.ApplyNewCodeCodeRanges(newCodeCodeRanges);
                    newCodeTrackerChanged = newCodeTrackerChanged || requiresChange;
                }
                changed = changed || newCodeTrackerChanged;
            }

            return changed;
        }

        private List<CodeSpanRange> GetNewCodeCodeRanges(
    List<CodeSpanRange> roslynCodeSpanRanges,
    List<CodeSpanRange> containingCodeTrackersCodeSpanRanges)
        {
            var newCodeCodeRanges = new List<CodeSpanRange>();
            int i = 0, j = 0;

            while (i < roslynCodeSpanRanges.Count && j < containingCodeTrackersCodeSpanRanges.Count)
            {
                var roslynRange = roslynCodeSpanRanges[i];
                var trackerRange = containingCodeTrackersCodeSpanRanges[j];

                if (roslynRange.EndLine < trackerRange.StartLine)
                {
                    // roslynRange does not intersect with trackerRange, add it to the result
                    newCodeCodeRanges.Add(roslynRange);
                    i++;
                }
                else if (roslynRange.StartLine > trackerRange.EndLine)
                {
                    // roslynRange is after trackerRange, move to the next trackerRange
                    j++;
                }
                else
                {
                    // roslynRange intersects with trackerRange, skip it
                    i++;
                }
            }

            // Add remaining roslynCodeSpanRanges that come after the last trackerRange
            while (i < roslynCodeSpanRanges.Count)
            {
                newCodeCodeRanges.Add(roslynCodeSpanRanges[i]);
                i++;
            }

            return newCodeCodeRanges;
        }



        public IEnumerable<IDynamicLine> GetLines(int startLineNumber, int endLineNumber)
        {
            List<int> lineNumbers = new List<int>();
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
                        lineNumbers.Add(line.Number);
                        yield return line;
                    }
                }
                if (done)
                {
                    break;
                }
            }
            var newLines = newCodeTracker?.Lines ?? Enumerable.Empty<IDynamicLine>();
            foreach (var line in newLines)
            {
                if (line.Number > endLineNumber)
                {
                    break;
                }
                if (line.Number >= startLineNumber)
                {
                    if(!lineNumbers.Contains(line.Number))
                    {
                        yield return line;
                    }
                }
            }
        }
        
    }

}
