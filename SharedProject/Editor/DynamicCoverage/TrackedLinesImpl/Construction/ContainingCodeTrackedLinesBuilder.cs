using FineCodeCoverage.Core.Utilities;
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
                /*
                    todo - https://learn.microsoft.com/en-us/previous-versions/t41260xs(v=vs.140)
                    non C++ https://learn.microsoft.com/en-us/dotnet/api/envdte80.filecodemodel2?view=visualstudiosdk-2022
                */
                return lines.Select(line => CreateSingleLineContainingCodeTracker(textSnapshot, line)).ToList();
            }
            return CreateRoslynContainingCodeTrackers(lines, textSnapshot, language == Language.CSharp);
        }

        IContainingCodeTracker CreateSingleLineContainingCodeTracker(ITextSnapshot textSnapshot,ILine line)
        {
            return CreateCoverageLines(textSnapshot, new List<ILine> { line}, CodeSpanRange.SingleLine(line.Number - 1));
        }

        private IContainingCodeTracker CreateOtherLines(ITextSnapshot textSnapshot, CodeSpanRange codeSpanRange)
        {
            return containingCodeTrackerFactory.CreateOtherLines(
                    textSnapshot,
                    codeSpanRange,
                    SpanTrackingMode.EdgeNegative
                );
        }

        private IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange)
        {
            return containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, lines, containingRange, SpanTrackingMode.EdgeExclusive);
        }

        private IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, CodeSpanRange containingRange)
        {
            return containingCodeTrackerFactory.CreateNotIncluded(textSnapshot, containingRange, SpanTrackingMode.EdgeExclusive);
            
        }

        private List<IContainingCodeTracker> CreateRoslynContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot,bool isCSharp)
        {
            List<IContainingCodeTracker> containingCodeTrackers = new List<IContainingCodeTracker>();
            var currentLine = 0;
            void CreateSingleLineContainingCodeTrackerInCase(ILine line)
            {
                // this should not happen - just in case missed something with Roslyn
                containingCodeTrackers.Add(CreateSingleLineContainingCodeTracker(textSnapshot, line));
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
                            CreateOtherLines(
                                textSnapshot,
                                CodeSpanRange.SingleLine(otherCodeLine)
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
                    containingCodeTracker = CreateCoverageLines(textSnapshot, containedLines, currentCodeSpanRange);
                }
                else
                {
                    containingCodeTracker = CreateNotIncluded(textSnapshot, currentCodeSpanRange);
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

        private ITrackedLines CreateCPPFromStates(List<SerializedState> states, ITextSnapshot currentSnapshot)
        {
            var numLines = currentSnapshot.LineCount;
            var cppContainingCodeTrackers = new List<IContainingCodeTracker>();
            foreach (var state in states)
            {
                var codeSpanRange = state.CodeSpanRange;
                if (codeSpanRange.EndLine < numLines)
                {
                    IContainingCodeTracker containingCodeTracker = null;
                    if (state.Lines.Count == 1 && state.Lines[0].CoverageType == DynamicCoverageType.Dirty)
                    {
                        containingCodeTracker = containingCodeTrackerFactory.CreateDirty(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive);
                    }
                    else
                    {
                        containingCodeTracker = CreateCoverageLines(currentSnapshot, state.Lines.Select(line => new AdjustedLine(line)).Cast<ILine>().ToList(), codeSpanRange);
                    }
                    cppContainingCodeTrackers.Add(containingCodeTracker);
                }
            }
            return containingCodeTrackedLinesFactory.Create(cppContainingCodeTrackers, null);
        }

        public ITrackedLines Create(string serializedCoverage, ITextSnapshot currentSnapshot, Language language)
        {
            var states = jsonConvertService.DeserializeObject<List<SerializedState>>(serializedCoverage);
            if(language == Language.CPP)
            {
               return CreateCPPFromStates(states, currentSnapshot);
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
                            containingCodeTracker = CreateOtherLines(currentSnapshot, codeSpanRange);
                            break;
                        case ContainingCodeTrackerType.NotIncluded:
                            containingCodeTracker = CreateNotIncluded(currentSnapshot, codeSpanRange);
                            break;
                        case ContainingCodeTrackerType.CoverageLines:
                            if(state.Lines.Count == 1 && state.Lines[0].CoverageType == DynamicCoverageType.Dirty)
                            {
                                containingCodeTracker = containingCodeTrackerFactory.CreateDirty(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive);
                            }
                            else
                            {
                                containingCodeTracker = CreateCoverageLines(currentSnapshot, state.Lines.Select(line => new AdjustedLine(line)).Cast<ILine>().ToList(), codeSpanRange);
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
            var states = trackedLinesImpl.ContainingCodeTrackers.Select(containingCodeTracker => SerializedState.From( containingCodeTracker.GetState())).ToList();
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
        
    }
    
}
