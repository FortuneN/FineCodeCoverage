using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System;
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
        private readonly IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory;
        private readonly INewCodeTrackerFactory newCodeTrackerFactory;
        private readonly IThreadHelper threadHelper;
        private readonly ITextSnapshotLineExcluder textSnapshotLineExcluder;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            IRoslynService roslynService,
            ILinesContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory,
            INewCodeTrackerFactory newCodeTrackerFactory,
            IThreadHelper threadHelper,
            ITextSnapshotLineExcluder textSnapshotLineExcluder
        )
        {
            this.roslynService = roslynService;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.containingCodeTrackedLinesFactory = containingCodeTrackedLinesFactory;
            this.newCodeTrackerFactory = newCodeTrackerFactory;
            this.threadHelper = threadHelper;
            this.textSnapshotLineExcluder = textSnapshotLineExcluder;
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
            var newCodeTracker = language == Language.CPP ? null : newCodeTrackerFactory.Create(language == Language.CSharp);
            return containingCodeTrackedLinesFactory.Create(containingCodeTrackers, newCodeTracker);
        }

        private List<IContainingCodeTracker> CreateContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            if (language == Language.CPP)
            {
                return lines.Select(line => CreateSingleLineContainingCodeTracker(textSnapshot, line,SpanTrackingMode.EdgeExclusive)).ToList();
            }
            return CreateRoslynContainingCodeTrackers(lines, textSnapshot, language == Language.CSharp);
        }

        IContainingCodeTracker CreateSingleLineContainingCodeTracker(ITextSnapshot textSnapshot,ILine line, SpanTrackingMode spanTrackingMode)
        {
            return containingCodeTrackerFactory.Create(textSnapshot, new List<ILine> { line}, new CodeSpanRange(line.Number, line.Number), spanTrackingMode);
        }

        private List<IContainingCodeTracker> CreateRoslynContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot,bool isCSharp)
        {
            List<IContainingCodeTracker> containingCodeTrackers = new List<IContainingCodeTracker>();
            var currentLine = 0;
            void CreateSingleLineContainingCodeTrackerInCase(ILine line)
            {
                // this should not happen - just in case missed something with Roslyn
                containingCodeTrackers.Add(CreateSingleLineContainingCodeTracker(textSnapshot, line, SpanTrackingMode.EdgeExclusive));
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
                if (to < currentLine) return;
                var otherCodeLines = Enumerable.Range(currentLine, to - currentLine + 1).Where(lineNumber =>
                {
                    
                    return !textSnapshotLineExcluder.ExcludeIfNotCode(textSnapshot, lineNumber, isCSharp);
                });
                foreach (var otherCodeLine in otherCodeLines)
                {
                    containingCodeTrackers.Add(
                            containingCodeTrackerFactory.CreateOtherLinesTracker(
                                textSnapshot,
                                new CodeSpanRange(otherCodeLine, otherCodeLine),
                                SpanTrackingMode.EdgeNegative
                            )
                    );
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
                    CreateSingleLineContainingCodeTrackerInCase(line);
                }
                else
                {
                    var adjustedLine = line.Number - 1;
                    if (adjustedLine < currentCodeSpanRange.StartLine)
                    {
                        CreateSingleLineContainingCodeTrackerInCase(line);
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
            TrackOtherLinesTo(textSnapshot.LineCount-1);
            return containingCodeTrackers;
        }

        private class CPPLine : ILine
        {
            public CPPLine(IDynamicLine dynamicLine)
            {
                Number = dynamicLine.Number + 1;
                switch(dynamicLine.CoverageType)
                {
                    case DynamicCoverageType.Covered:
                        CoverageType = CoverageType.Covered;
                        break;
                    case DynamicCoverageType.NotCovered:
                        CoverageType = CoverageType.NotCovered;
                        break;
                    case DynamicCoverageType.Partial:
                        CoverageType = CoverageType.Partial;
                        break;
                    default:
                        throw new ArgumentException("");//todo
                }
            }

            public int Number { get; }

            public CoverageType CoverageType { get; }
        }
    }
    
}
