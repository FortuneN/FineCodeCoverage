using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;
using FineCodeCoverage.Engine.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITrackedLinesFactory))]
    internal class ContainingCodeTrackedLinesBuilder : ITrackedLinesFactory
    {
        private readonly ICoverageContentType[] coverageContentTypes;
        private readonly ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory;
        private readonly IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory;
        private readonly INewCodeTrackerFactory newCodeTrackerFactory;
        private readonly IJsonConvertService jsonConvertService;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            [ImportMany]
            ICoverageContentType[] coverageContentTypes,
            ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory,
            INewCodeTrackerFactory newCodeTrackerFactory,
            IJsonConvertService jsonConvertService
        )
        {
            this.coverageContentTypes = coverageContentTypes;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.containingCodeTrackedLinesFactory = containingCodeTrackedLinesFactory;
            this.newCodeTrackerFactory = newCodeTrackerFactory;
            this.jsonConvertService = jsonConvertService;
        }

        private ICoverageContentType GetCoverageContentType(ITextSnapshot textSnapshot)
        {
            string contentTypeName = textSnapshot.TextBuffer.ContentType.TypeName;
            return this.coverageContentTypes.First(
                coverageContentType => coverageContentType.ContentTypeName == contentTypeName);
        }

        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot)
        {
            ICoverageContentType coverageContentType = this.GetCoverageContentType(textSnapshot);
            IFileCodeSpanRangeService fileCodeSpanRangeService = coverageContentType.FileCodeSpanRangeService;
            List<IContainingCodeTracker> containingCodeTrackers = this.CreateContainingCodeTrackers(
                lines, textSnapshot, fileCodeSpanRangeService);
            ILineExcluder lineExcluder = coverageContentType.LineExcluder;
            INewCodeTracker newCodeTracker = lineExcluder == null ? null : this.newCodeTrackerFactory.Create(lineExcluder);
            return this.containingCodeTrackedLinesFactory.Create(
                containingCodeTrackers, 
                newCodeTracker, 
                coverageContentType.FileCodeSpanRangeServiceForChanges);
        }

        private List<IContainingCodeTracker> CreateContainingCodeTrackers(List<ILine> lines, ITextSnapshot textSnapshot, IFileCodeSpanRangeService fileCodeSpanRangeService) 
            => fileCodeSpanRangeService == null
                ? lines.Select(line => this.CreateSingleLineContainingCodeTracker(textSnapshot, line)).ToList()
                : this.CreateContainingCodeTrackersFromCodeSpanRanges(lines, textSnapshot, fileCodeSpanRangeService.GetFileCodeSpanRanges(textSnapshot));

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

        private List<IContainingCodeTracker> CreateContainingCodeTrackersFromCodeSpanRanges(List<ILine> lines, ITextSnapshot textSnapshot, List<CodeSpanRange> codeSpanRanges)
        {
            var containingCodeTrackers = new List<IContainingCodeTracker>();
            int currentLine = 0;
            // this should not happen - just in case missed
            void CreateSingleLineContainingCodeTrackerInCase(ILine line)
                => containingCodeTrackers.Add(this.CreateSingleLineContainingCodeTracker(textSnapshot, line));

            int currentCodeSpanIndex = -1;
            CodeSpanRange currentCodeSpanRange = null;
            SetNextCodeSpanRange();
            var containedLines = new List<ILine>();

            void SetNextCodeSpanRange()
            {
                currentCodeSpanIndex++;
                CodeSpanRange previousCodeSpanRange = currentCodeSpanRange;
                currentCodeSpanRange = currentCodeSpanIndex < codeSpanRanges.Count
                    ? codeSpanRanges[currentCodeSpanIndex]
                    : null;
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
                IEnumerable<int> otherCodeLines = Enumerable.Range(currentLine, to - currentLine + 1);
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

        private ITrackedLines RecreateTrackedLinesFromStates(List<SerializedState> states, ITextSnapshot currentSnapshot)
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

        private ITrackedLines RecreateTrackedLinesFromCoverageContentType(
            List<SerializedState> states, 
            ITextSnapshot currentSnapshot,
            ICoverageContentType coverageContentType)
        {
            IFileCodeSpanRangeService fileCodeSpanRangeServiceForChanges = coverageContentType.FileCodeSpanRangeServiceForChanges;
            List<CodeSpanRange> codeSpanRanges = coverageContentType.FileCodeSpanRangeService.GetFileCodeSpanRanges(currentSnapshot);
            List<IContainingCodeTracker> containingCodeTrackers = this.RecreateContainingCodeTrackersWithUnchangedCodeSpanRange(codeSpanRanges, states, currentSnapshot);
            IEnumerable<int> newCodeLineNumbers = this.GetRecreateNewCodeLineNumbers(codeSpanRanges, fileCodeSpanRangeServiceForChanges != null);
            INewCodeTracker newCodeTracker = coverageContentType.LineExcluder == null ? null : this.newCodeTrackerFactory.Create(coverageContentType.LineExcluder, newCodeLineNumbers, currentSnapshot);
            return this.containingCodeTrackedLinesFactory.Create(containingCodeTrackers, newCodeTracker, coverageContentType.FileCodeSpanRangeServiceForChanges);
        }

        private IEnumerable<int> GetRecreateNewCodeLineNumbers(List<CodeSpanRange> newCodeCodeRanges, bool hasFileCodeSpanRangeServiceForChanges)
            => hasFileCodeSpanRangeServiceForChanges
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
        public ITrackedLines Create(string serializedCoverage, ITextSnapshot currentSnapshot)
        {
           
            List<SerializedState> states = this.jsonConvertService.DeserializeObject<List<SerializedState>>(serializedCoverage);
            ICoverageContentType coverageContextType = this.GetCoverageContentType(currentSnapshot);
            IFileCodeSpanRangeService fileCodeSpanRangeService = coverageContextType.FileCodeSpanRangeService;
            return fileCodeSpanRangeService == null
                ? this.RecreateTrackedLinesFromStates(states, currentSnapshot)
                : this.RecreateTrackedLinesFromCoverageContentType(states, currentSnapshot, coverageContextType);
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
