using FineCodeCoverage.Core.Utilities;
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
    internal static class ContainingCodeTrackerStateExtensions
    {
        public static SerializedState CreateSerialized(this ContainingCodeTrackerState containingCodeTrackerState)
        {
            return new SerializedState(
                containingCodeTrackerState.CodeSpanRange,
                containingCodeTrackerState.Type,
                containingCodeTrackerState.Lines.Select(line => new DynamicLine(line.Number, line.CoverageType)).ToList()
             );
        }
    }

    internal class SerializedState
    {
        public SerializedState(CodeSpanRange codeSpanRange, ContainingCodeTrackerType type, List<DynamicLine> dynamicLines)
        {
            CodeSpanRange = codeSpanRange;
            Type = type;
            Lines = dynamicLines;
        }

        public CodeSpanRange CodeSpanRange { get; set; }
        public ContainingCodeTrackerType Type { get; set; }
        public List<DynamicLine> Lines { get; set; }
    }

    [Export(typeof(ITrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesBuilder : ITrackedLinesFactory
    {
        private readonly IRoslynService roslynService;
        private readonly ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory;
        private readonly IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory;
        private readonly INewCodeTrackerFactory newCodeTrackerFactory;
        private readonly IThreadHelper threadHelper;
        private readonly ITextSnapshotLineExcluder textSnapshotLineExcluder;
        private readonly IJsonConvertService jsonConvertService;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            IRoslynService roslynService,
            ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory,
            INewCodeTrackerFactory newCodeTrackerFactory,
            IThreadHelper threadHelper,
            ITextSnapshotLineExcluder textSnapshotLineExcluder,
            IJsonConvertService jsonConvertService
        )
        {
            this.roslynService = roslynService;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.containingCodeTrackedLinesFactory = containingCodeTrackedLinesFactory;
            this.newCodeTrackerFactory = newCodeTrackerFactory;
            this.threadHelper = threadHelper;
            this.textSnapshotLineExcluder = textSnapshotLineExcluder;
            this.jsonConvertService = jsonConvertService;
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
            return containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { line}, CodeSpanRange.SingleLine(line.Number - 1), spanTrackingMode);
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
                            containingCodeTrackerFactory.CreateOtherLines(
                                textSnapshot,
                                CodeSpanRange.SingleLine(otherCodeLine),
                                SpanTrackingMode.EdgeNegative
                            )
                    );
                }
            }

            void CreateRangeContainingCodeTracker()
            {
                TrackOtherLines();
                IContainingCodeTracker containingCodeTracker;
                if(containedLines.Count > 0)
                {
                    containingCodeTracker = containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, containedLines, currentCodeSpanRange, SpanTrackingMode.EdgeExclusive);
                }
                else
                {
                    containingCodeTracker = containingCodeTrackerFactory.CreateNotIncluded(textSnapshot, currentCodeSpanRange, SpanTrackingMode.EdgeExclusive);
                }
                containingCodeTrackers.Add(containingCodeTracker);
                    
                
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

        public ITrackedLines Create(string serializedCoverage, ITextSnapshot currentSnapshot, Language language)
        {
            var states = jsonConvertService.DeserializeObject<List<SerializedState>>(serializedCoverage);
            if(language == Language.CPP)
            {
                //todo
            }
            var roslynContainingCodeSpans = threadHelper.JoinableTaskFactory.Run(() => roslynService.GetContainingCodeSpansAsync(currentSnapshot));
            var codeSpanRanges = roslynContainingCodeSpans.Select(roslynCodeSpan => GetCodeSpanRange(roslynCodeSpan, currentSnapshot)).ToList();
            List<IContainingCodeTracker> containingCodeTrackers = new List<IContainingCodeTracker>();
            foreach (var state in states)
            {
                var codeSpanRange = state.CodeSpanRange;
                var removed = codeSpanRanges.Remove(codeSpanRange);
                if (removed)
                {
                    IContainingCodeTracker containingCodeTracker = null;
                    switch (state.Type)
                    {
                        case ContainingCodeTrackerType.OtherLines:
                            containingCodeTracker = containingCodeTrackerFactory.CreateOtherLines(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeNegative);
                            break;
                        case ContainingCodeTrackerType.NotIncluded:
                            containingCodeTracker = containingCodeTrackerFactory.CreateNotIncluded(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive);
                            break;
                        case ContainingCodeTrackerType.CoverageLines:
                            if(state.Lines.Count == 1 && state.Lines[0].CoverageType == DynamicCoverageType.Dirty)
                            {
                                containingCodeTracker = containingCodeTrackerFactory.CreateDirty(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive);
                            }
                            else
                            {
                                containingCodeTracker = containingCodeTrackerFactory.CreateCoverageLines(currentSnapshot, state.Lines.Select(line => new AdjustedLine(line)).Cast<ILine>().ToList(), codeSpanRange, SpanTrackingMode.EdgeExclusive);
                            }
                            break;
                    }
                    containingCodeTrackers.Add(containingCodeTracker);
                }
            }
            var newCodeTracker = newCodeTrackerFactory.Create(language == Language.CSharp,codeSpanRanges, currentSnapshot);
            return containingCodeTrackedLinesFactory.Create(containingCodeTrackers, newCodeTracker);
        }

        public string Serialize(ITrackedLines trackedLines)
        {
            var trackedLinesImpl = trackedLines as TrackedLines;
            var states = trackedLinesImpl.ContainingCodeTrackers.Select(containingCodeTracker => containingCodeTracker.GetState().CreateSerialized()).ToList();
            return jsonConvertService.SerializeObject(states);
        }

        private class AdjustedLine : ILine
        {
            public AdjustedLine(IDynamicLine dynamicLine)
            {
                Number = dynamicLine.Number + 1;
                CoverageType = DynamicCoverageTypeConverter.Convert(dynamicLine.CoverageType);
            }

            public int Number { get; }

            public CoverageType CoverageType { get; }
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
