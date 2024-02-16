using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesBuilder : ITrackedLinesFactory
    {
        private readonly IRoslynService roslynService;
        private readonly ILinesContainingCodeTrackerFactory containingCodeTrackerFactory;
        private readonly IContainingCodeTrackedLinesFactory trackedLinesFactory;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            IRoslynService roslynService,
            ILinesContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory trackedLinesFactory,
            IThreadHelper threadHelper
        )
        {
            this.roslynService = roslynService;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.trackedLinesFactory = trackedLinesFactory;
            this.threadHelper = threadHelper;
        }

        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            var startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            var endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }
        
        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            var containingCodeTrackers = CreateContainingCodeTrackers(lines, textSnapshot, language);
            return trackedLinesFactory.Create(containingCodeTrackers, new NewCodeTracker(language == Language.CSharp));
        }

        private List<IContainingCodeTracker> CreateContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            if( lines.Count == 0 ) return Enumerable.Empty<IContainingCodeTracker>().ToList();

            if (language == Language.CPP)
            {
                return lines.Select(line => containingCodeTrackerFactory.Create(textSnapshot, line,SpanTrackingMode.EdgeExclusive)).ToList();
            }
            var isCSharp = language == Language.CSharp;
            return CreateRoslynContainingCodeTrackers(lines, textSnapshot,isCSharp);
            
        }

        private List<IContainingCodeTracker> CreateRoslynContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot,bool isCSharp)
        {
            List<IContainingCodeTracker> containingCodeTrackers = new List<IContainingCodeTracker>();
            var currentLine = 0;
            void CreateSingleLineContainingCodeTracker(ILine line)
            {
                // this should not happen - just in case missed something with Roslyn
                containingCodeTrackers.Add(containingCodeTrackerFactory.Create(textSnapshot, line, SpanTrackingMode.EdgeExclusive));
            }
           
            var roslynContainingCodeSpans = threadHelper.JoinableTaskFactory.Run(() => roslynService.GetContainingCodeSpansAsync(textSnapshot));
            var currentCodeSpanIndex = -1;
            CodeSpanRange currentCodeSpanRange = null;
            SetNextCodeSpanRange();
            var containedLines = new List<ILine>();

            void SetNextCodeSpanRange()
            {
                currentCodeSpanIndex++;
                if (currentCodeSpanIndex < roslynContainingCodeSpans.Count)
                {
                    currentCodeSpanRange = GetCodeSpanRange(roslynContainingCodeSpans[currentCodeSpanIndex], textSnapshot);
                }
                else
                {
                    currentCodeSpanRange = null;
                }
            }

            void TrackOtherLines()
            {
                var to = currentCodeSpanRange.StartLine - 1;
                TrackOtherLinesTo(to);
                currentLine = currentCodeSpanRange.EndLine + 1;
            }

            void TrackOtherLinesTo(int to)
            {
                var additionalCodeLines = Enumerable.Range(currentLine, to - currentLine).Where(lineNumber => !CodeLineExcluder.ExcludeIfNotCode(textSnapshot.GetLineFromLineNumber(lineNumber).Extent, isCSharp)).ToList();
                if (additionalCodeLines.Any())
                {
                    var uncontainedCodeSpanRange = new CodeSpanRange(additionalCodeLines[0], additionalCodeLines.Last());
                    containingCodeTrackers.Add(
                        containingCodeTrackerFactory.Create(textSnapshot, Enumerable.Empty<ILine>().ToList(), uncontainedCodeSpanRange, SpanTrackingMode.EdgeNegative));
                }
            }

            void CreateRangeContainingCodeTracker()
            {
                TrackOtherLines();
                containingCodeTrackers.Add(
                    containingCodeTrackerFactory.Create(textSnapshot, containedLines, currentCodeSpanRange,SpanTrackingMode.EdgeExclusive));
                
                containedLines = new List<ILine>();
                SetNextCodeSpanRange();
            }

            void LineAction(ILine line)
            {
                if (currentCodeSpanRange == null)
                {
                    CreateSingleLineContainingCodeTracker(line);
                }
                else
                {
                    var adjustedLine = line.Number - 1;
                    if (adjustedLine < currentCodeSpanRange.StartLine)
                    {
                        CreateSingleLineContainingCodeTracker(line);
                    }
                    else if (adjustedLine > currentCodeSpanRange.EndLine)
                    {
                        CreateRangeContainingCodeTracker();
                        
                        LineAction(line);

                    }
                    else
                    {
                        containedLines.Add(line);
                    }
                }
            }

            foreach (var line in lines) // these are in order`
            {
                LineAction(line);
            }

            while (currentCodeSpanRange != null)
            {
                CreateRangeContainingCodeTracker();
            }
            TrackOtherLinesTo(textSnapshot.LineCount);
            return containingCodeTrackers;
        }
    }
}
