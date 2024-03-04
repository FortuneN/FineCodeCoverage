using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesBuilder : ITrackedLinesFactory, IFileCodeSpanRangeService
    {
        private readonly IRoslynService roslynService;
        private readonly ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory;
        private readonly IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory;
        private readonly INewCodeTrackerFactory newCodeTrackerFactory;
        private readonly IThreadHelper threadHelper;
        private readonly ITextSnapshotLineExcluder textSnapshotLineExcluder;
        private readonly IJsonConvertService jsonConvertService;
        private readonly IAppOptionsProvider appOptionsProvider;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            IRoslynService roslynService,
            ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory,
            INewCodeTrackerFactory newCodeTrackerFactory,
            IThreadHelper threadHelper,
            ITextSnapshotLineExcluder textSnapshotLineExcluder,
            IJsonConvertService jsonConvertService,
            IAppOptionsProvider appOptionsProvider
        )
        {
            this.roslynService = roslynService;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.containingCodeTrackedLinesFactory = containingCodeTrackedLinesFactory;
            this.newCodeTrackerFactory = newCodeTrackerFactory;
            this.threadHelper = threadHelper;
            this.textSnapshotLineExcluder = textSnapshotLineExcluder;
            this.jsonConvertService = jsonConvertService;
            this.appOptionsProvider = appOptionsProvider;
        }

        private bool UseRoslynWhenTextChanges()
            => this.appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.UseRoslynWhenTextChanges;

        private CodeSpanRange GetCodeSpanRange(TextSpan span, ITextSnapshot textSnapshot)
        {
            int startLine = textSnapshot.GetLineNumberFromPosition(span.Start);
            int endLine = textSnapshot.GetLineNumberFromPosition(span.End);
            return new CodeSpanRange(startLine, endLine);
        }

        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            List<IContainingCodeTracker> containingCodeTrackers = this.CreateContainingCodeTrackers(lines, textSnapshot, language);
            INewCodeTracker newCodeTracker = language == Language.CPP ? null : this.newCodeTrackerFactory.Create(language == Language.CSharp);
            IFileCodeSpanRangeService fileCodeSpanRangeService = this.GetFileCodeSpanRangeService(language);
            return this.containingCodeTrackedLinesFactory.Create(containingCodeTrackers, newCodeTracker, fileCodeSpanRangeService);
        }

        private IFileCodeSpanRangeService GetFileCodeSpanRangeService(Language language)
            => language == Language.CPP ? null : this.GetRoslynFileCodeSpanRangeService(this.UseRoslynWhenTextChanges());

        private IFileCodeSpanRangeService GetRoslynFileCodeSpanRangeService(bool useRoslynWhenTextChanges)
            => useRoslynWhenTextChanges ? this : null;

        private List<IContainingCodeTracker> CreateContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, Language language)
        {
            if (language == Language.CPP)
            {
                /*
                    todo - https://learn.microsoft.com/en-us/previous-versions/t41260xs(v=vs.140)
                    non C++ https://learn.microsoft.com/en-us/dotnet/api/envdte80.filecodemodel2?view=visualstudiosdk-2022
                */
                return lines.Select(line => this.CreateSingleLineContainingCodeTracker(textSnapshot, line)).ToList();
            }

            return this.CreateRoslynContainingCodeTrackers(lines, textSnapshot, language == Language.CSharp);
        }

        private IContainingCodeTracker CreateSingleLineContainingCodeTracker(ITextSnapshot textSnapshot, ILine line)
            => this.CreateCoverageLines(textSnapshot, new List<ILine> { line }, CodeSpanRange.SingleLine(line.Number - 1));

        private IContainingCodeTracker CreateOtherLines(ITextSnapshot textSnapshot, CodeSpanRange codeSpanRange)
            => this.containingCodeTrackerFactory.CreateOtherLines(
                    textSnapshot,
                    codeSpanRange,
                    SpanTrackingMode.EdgeNegative
                );

        private IContainingCodeTracker CreateCoverageLines(ITextSnapshot textSnapshot, List<ILine> lines, CodeSpanRange containingRange)
            => this.containingCodeTrackerFactory.CreateCoverageLines(textSnapshot, lines, containingRange, SpanTrackingMode.EdgeExclusive);

        private IContainingCodeTracker CreateNotIncluded(ITextSnapshot textSnapshot, CodeSpanRange containingRange)
            => this.containingCodeTrackerFactory.CreateNotIncluded(textSnapshot, containingRange, SpanTrackingMode.EdgeExclusive);

        private List<IContainingCodeTracker> CreateRoslynContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, bool isCSharp)
        {
            var containingCodeTrackers = new List<IContainingCodeTracker>();
            int currentLine = 0;
            // this should not happen - just in case missed something with Roslyn
            void CreateSingleLineContainingCodeTrackerInCase(ILine line)
                => containingCodeTrackers.Add(this.CreateSingleLineContainingCodeTracker(textSnapshot, line));

            List<TextSpan> roslynContainingCodeSpans = this.threadHelper.JoinableTaskFactory.Run(() => this.roslynService.GetContainingCodeSpansAsync(textSnapshot));
            int currentCodeSpanIndex = -1;
            CodeSpanRange currentCodeSpanRange = null;
            SetNextCodeSpanRange();
            var containedLines = new List<ILine>();

            void SetNextCodeSpanRange()
            {
                currentCodeSpanIndex++;
                CodeSpanRange previousCodeSpanRange = currentCodeSpanRange;
                currentCodeSpanRange = currentCodeSpanIndex < roslynContainingCodeSpans.Count
                    ? this.GetCodeSpanRange(roslynContainingCodeSpans[currentCodeSpanIndex], textSnapshot)
                    : null;
                if (currentCodeSpanRange != null && previousCodeSpanRange != null && previousCodeSpanRange.Equals(currentCodeSpanRange))
                {
                    SetNextCodeSpanRange();
                }
            }

            void TrackOtherLines()
            {
                int to = currentCodeSpanRange.StartLine - 1;
                TrackOtherLinesTo(to);
                currentLine = currentCodeSpanRange.EndLine + 1;
            }

            void TrackOtherLinesTo(int to)
            {
                if (to < currentLine) return;
                IEnumerable<int> otherCodeLines = Enumerable.Range(currentLine, to - currentLine + 1)
                    .Where(lineNumber => !this.textSnapshotLineExcluder.ExcludeIfNotCode(textSnapshot, lineNumber, isCSharp));
                foreach (int otherCodeLine in otherCodeLines)
                {
                    containingCodeTrackers.Add(
                            this.CreateOtherLines(
                                textSnapshot,
                                CodeSpanRange.SingleLine(otherCodeLine)
                            )
                    );
                }
            }

            void CreateRangeContainingCodeTracker()
            {
                TrackOtherLines();
                IContainingCodeTracker containingCodeTracker = containedLines.Count > 0
                    ? this.CreateCoverageLines(textSnapshot, containedLines, currentCodeSpanRange)
                    : this.CreateNotIncluded(textSnapshot, currentCodeSpanRange);
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
                    int adjustedLine = line.Number - 1;
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

            foreach (ILine line in lines) // these are in order`
            {
                LineAction(line);
            }

            while (currentCodeSpanRange != null)
            {
                CreateRangeContainingCodeTracker();
            }

            TrackOtherLinesTo(textSnapshot.LineCount - 1);
            return containingCodeTrackers;
        }

        private ITrackedLines RecreateTrackedLinesFromCPPStates(List<SerializedState> states, ITextSnapshot currentSnapshot)
        {
            var containingCodeTrackers = this.StatesWithinSnapshot(states, currentSnapshot)
                .Select(state => this.RecreateCoverageLines(state, currentSnapshot)).ToList();
            return this.containingCodeTrackedLinesFactory.Create(containingCodeTrackers, null, null);
        }

        private IEnumerable<SerializedState> StatesWithinSnapshot(IEnumerable<SerializedState> states, ITextSnapshot currentSnapshot)
        {
            int numLines = currentSnapshot.LineCount;
            return states.Where(state => state.CodeSpanRange.EndLine < numLines);
        }

        private IContainingCodeTracker RecreateCoverageLines(SerializedState state, ITextSnapshot currentSnapshot)
        {
            CodeSpanRange codeSpanRange = state.CodeSpanRange;
            return state.Lines[0].CoverageType == DynamicCoverageType.Dirty
                ? this.containingCodeTrackerFactory.CreateDirty(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive)
                : this.CreateCoverageLines(currentSnapshot, this.AdjustCoverageLines(state.Lines), codeSpanRange);
        }

        private List<ILine> AdjustCoverageLines(List<DynamicLine> dynamicLines)
            => dynamicLines.Select(dynamicLine => new AdjustedLine(dynamicLine)).Cast<ILine>().ToList();

        private List<CodeSpanRange> GetRoslynCodeSpanRanges(ITextSnapshot currentSnapshot)
        {
            List<TextSpan> roslynContainingCodeSpans = this.threadHelper.JoinableTaskFactory.Run(() => this.roslynService.GetContainingCodeSpansAsync(currentSnapshot));
            return roslynContainingCodeSpans.Select(roslynCodeSpan => this.GetCodeSpanRange(roslynCodeSpan, currentSnapshot)).ToList();
        }

        private List<IContainingCodeTracker> RecreateContainingCodeTrackersWithUnchangedCodeSpanRange(
            List<CodeSpanRange> codeSpanRanges,
            List<SerializedState> states,
            ITextSnapshot currentSnapshot
        ) => states.Where(state => codeSpanRanges.Remove(state.CodeSpanRange))
            .Select(state => this.RecreateContainingCodeTracker(state, currentSnapshot)).ToList();

        private IContainingCodeTracker RecreateContainingCodeTracker(SerializedState state, ITextSnapshot currentSnapshot)
        {
            CodeSpanRange codeSpanRange = state.CodeSpanRange;
            IContainingCodeTracker containingCodeTracker = null;
            switch (state.Type)
            {
                case ContainingCodeTrackerType.OtherLines:
                    containingCodeTracker = this.CreateOtherLines(currentSnapshot, codeSpanRange);
                    break;
                case ContainingCodeTrackerType.NotIncluded:
                    containingCodeTracker = this.CreateNotIncluded(currentSnapshot, codeSpanRange);
                    break;
                case ContainingCodeTrackerType.CoverageLines:
                    containingCodeTracker = this.RecreateCoverageLines(state, currentSnapshot);
                    break;
            }

            return containingCodeTracker;
        }

        private ITrackedLines RecreateTrackedLinesFromRoslynState(List<SerializedState> states, ITextSnapshot currentSnapshot, bool isCharp)
        {
            bool useRoslynWhenTextChanges = this.UseRoslynWhenTextChanges();
            IFileCodeSpanRangeService roslynFileCodeSpanRangeService = this.GetRoslynFileCodeSpanRangeService(useRoslynWhenTextChanges);
            List<CodeSpanRange> codeSpanRanges = this.GetRoslynCodeSpanRanges(currentSnapshot);
            List<IContainingCodeTracker> containingCodeTrackers = this.RecreateContainingCodeTrackersWithUnchangedCodeSpanRange(codeSpanRanges, states, currentSnapshot);
            IEnumerable<int> newCodeLineNumbers = this.GetRecreateNewCodeLineNumbers(codeSpanRanges, useRoslynWhenTextChanges);
            INewCodeTracker newCodeTracker = this.newCodeTrackerFactory.Create(isCharp, newCodeLineNumbers, currentSnapshot);

            return this.containingCodeTrackedLinesFactory.Create(containingCodeTrackers, newCodeTracker, roslynFileCodeSpanRangeService);
        }

        private IEnumerable<int> GetRecreateNewCodeLineNumbers(List<CodeSpanRange> newCodeCodeRanges, bool useRoslynWhenTextChanges)
            => useRoslynWhenTextChanges
                ? this.StartLines(newCodeCodeRanges)
                : this.EveryLineInCodeSpanRanges(newCodeCodeRanges);

        private IEnumerable<int> StartLines(List<CodeSpanRange> newCodeCodeRanges)
            => newCodeCodeRanges.Select(newCodeCodeRange => newCodeCodeRange.StartLine);
        private IEnumerable<int> EveryLineInCodeSpanRanges(List<CodeSpanRange> newCodeCodeRanges)
            => newCodeCodeRanges.SelectMany(
                newCodeCodeRange => Enumerable.Range(
                    newCodeCodeRange.StartLine,
                    newCodeCodeRange.EndLine - newCodeCodeRange.StartLine + 1)
                );
        public ITrackedLines Create(string serializedCoverage, ITextSnapshot currentSnapshot, Language language)
        {
            List<SerializedState> states = this.jsonConvertService.DeserializeObject<List<SerializedState>>(serializedCoverage);
            return language == Language.CPP
                ? this.RecreateTrackedLinesFromCPPStates(states, currentSnapshot)
                : this.RecreateTrackedLinesFromRoslynState(states, currentSnapshot, language == Language.CSharp);
        }

        public string Serialize(ITrackedLines trackedLines)
        {
            var trackedLinesImpl = trackedLines as TrackedLines;
            List<SerializedState> states = this.GetSerializedStates(trackedLinesImpl);
            return this.jsonConvertService.SerializeObject(states);
        }

        private List<SerializedState> GetSerializedStates(TrackedLines trackedLines)
            => trackedLines.ContainingCodeTrackers.Select(
                containingCodeTracker => SerializedState.From(containingCodeTracker.GetState())).ToList();

        public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot) => this.GetRoslynCodeSpanRanges(snapshot);

        private class AdjustedLine : ILine
        {
            public AdjustedLine(IDynamicLine dynamicLine)
            {
                this.Number = dynamicLine.Number + 1;
                this.CoverageType = DynamicCoverageTypeConverter.Convert(dynamicLine.CoverageType);
            }

            public int Number { get; }

            public CoverageType CoverageType { get; }
        }
    }
}
