using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;
using FineCodeCoverage.Engine.Model;
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
        private readonly ITextSnapshotText textSnapshotText;

        [ImportingConstructor]
        public ContainingCodeTrackedLinesBuilder(
            [ImportMany]
            ICoverageContentType[] coverageContentTypes,
            ICodeSpanRangeContainingCodeTrackerFactory containingCodeTrackerFactory,
            IContainingCodeTrackedLinesFactory containingCodeTrackedLinesFactory,
            INewCodeTrackerFactory newCodeTrackerFactory,
            IJsonConvertService jsonConvertService,
            ITextSnapshotText textSnapshotText
        )
        {
            this.coverageContentTypes = coverageContentTypes;
            this.containingCodeTrackerFactory = containingCodeTrackerFactory;
            this.containingCodeTrackedLinesFactory = containingCodeTrackedLinesFactory;
            this.newCodeTrackerFactory = newCodeTrackerFactory;
            this.jsonConvertService = jsonConvertService;
            this.textSnapshotText = textSnapshotText;
        }

        private ICoverageContentType GetCoverageContentType(ITextSnapshot textSnapshot)
        {
            string contentTypeName = textSnapshot.ContentType.TypeName;
            return this.coverageContentTypes.First(
                coverageContentType => coverageContentType.ContentTypeName == contentTypeName);
        }

        private IFileCodeSpanRangeService GetFileCodeSpanRangeServiceForChanges(ICoverageContentType coverageContentType)
            => coverageContentType.UseFileCodeSpanRangeServiceForChanges ? coverageContentType.FileCodeSpanRangeService : null;

        private INewCodeTracker GetNewCodeTrackerIfProvidesLineExcluder(ILineExcluder lineExcluder) 
            => lineExcluder == null ? null : this.newCodeTrackerFactory.Create(lineExcluder);

        public ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot)
        {
            ICoverageContentType coverageContentType = this.GetCoverageContentType(textSnapshot);
            IFileCodeSpanRangeService fileCodeSpanRangeService = coverageContentType.FileCodeSpanRangeService;
            (List<IContainingCodeTracker> containingCodeTrackers, bool usedFileCodeSpanRangeService) = this.CreateContainingCodeTrackers(
                lines, textSnapshot, fileCodeSpanRangeService, coverageContentType.CoverageOnlyFromFileCodeSpanRangeService);

            IContainingCodeTrackerTrackedLines trackedLines = containingCodeTrackers == null
                ? this.GetNonTrackingTrackedLines()
                : this.containingCodeTrackedLinesFactory.Create(
                containingCodeTrackers,
                this.GetNewCodeTrackerIfProvidesLineExcluder(coverageContentType.LineExcluder),
                this.GetFileCodeSpanRangeServiceForChanges(coverageContentType));

            return new ContainingCodeTrackerTrackedLinesWithState(trackedLines, usedFileCodeSpanRangeService);
        }

        private IContainingCodeTrackerTrackedLines GetNonTrackingTrackedLines() => this.containingCodeTrackedLinesFactory.Create(new List<IContainingCodeTracker>(), null, null);

        private (List<IContainingCodeTracker> containingCodeTrackers, bool usedFileCodeSpanRangeService) CreateContainingCodeTrackers(
            List<ILine> lines, 
            ITextSnapshot textSnapshot, 
            IFileCodeSpanRangeService fileCodeSpanRangeService,
            bool coverageOnlyFromFileCodeSpanRangeService
        )
        {
            if (this.AnyLinesOutsideTextSnapshot(lines, textSnapshot))
            {
                return (null, false);
            }

            if (fileCodeSpanRangeService != null)
            {
                List<CodeSpanRange> codeSpanRanges = fileCodeSpanRangeService.GetFileCodeSpanRanges(textSnapshot);
                if (codeSpanRanges != null)
                {
                    return (this.CreateContainingCodeTrackersFromCodeSpanRanges(
                        lines, textSnapshot, codeSpanRanges, coverageOnlyFromFileCodeSpanRangeService), true);
                }
            }

            return (lines.Select(line => this.CreateSingleLineContainingCodeTracker(textSnapshot, line)).ToList(), false);
        }

        private bool AnyLinesOutsideTextSnapshot(List<ILine> lines, ITextSnapshot textSnapshot)
            => lines.Any(line => line.Number - 1 >= textSnapshot.LineCount);

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

        private List<IContainingCodeTracker> CreateContainingCodeTrackersFromCodeSpanRanges(
            List<ILine> lines,
            ITextSnapshot textSnapshot,
            List<CodeSpanRange> codeSpanRanges,
            bool coverageOnlyFromFileCodeSpanRangeService
        )
        {
            var containingCodeTrackers = new List<IContainingCodeTracker>();
            Func<T> GetNextCreator<T>(List<T> list)
            {
                T GetNext()
                {
                    T next = list.FirstOrDefault();
                    if (next != null)
                    {
                        list.RemoveAt(0);
                    }

                    return next;
                }

                return GetNext;
            }

            Func<ILine> GetNextLine = GetNextCreator(lines);
            Func<CodeSpanRange> GetNextCodeSpanRange = GetNextCreator(codeSpanRanges);
            

            ILine line = GetNextLine();
            CodeSpanRange codeSpanRange = GetNextCodeSpanRange();
            var containedLines = new List<ILine>();
            bool InCodeSpanRange(int lineNumber) => codeSpanRange != null && codeSpanRange.StartLine <= lineNumber && codeSpanRange.EndLine >= lineNumber;
            bool AtEndOfCodeSpanRange(int lineNumber) => codeSpanRange != null && codeSpanRange.EndLine == lineNumber;
            bool LineAtLineNumber(int lineNumber) => line != null && line.Number - 1 == lineNumber;
            void CreateOtherLine(int otherCodeLine)
            {
                string lineText = this.textSnapshotText.GetLineText(textSnapshot, otherCodeLine);
                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    containingCodeTrackers.Add(
                            this.CreateOtherLines(
                                textSnapshot,
                                CodeSpanRange.SingleLine(otherCodeLine)
                            )
                    );
                }
            }

            for (int lineNumber = 0; lineNumber < textSnapshot.LineCount; lineNumber++)
            {
                bool inCodeSpanRange = InCodeSpanRange(lineNumber);
                if(LineAtLineNumber(lineNumber))
                {
                    if(inCodeSpanRange)
                    {
                        containedLines.Add(line);
                    }
                    else
                    {
                        if (!coverageOnlyFromFileCodeSpanRangeService)
                        {
                            containingCodeTrackers.Add(this.CreateSingleLineContainingCodeTracker(textSnapshot, line));
                        }
                        else
                        {
                            CreateOtherLine(lineNumber);
                        }
                    }

                    line = GetNextLine();
                }
                else if (!inCodeSpanRange)
                {
                    CreateOtherLine(lineNumber);
                }

                if (AtEndOfCodeSpanRange(lineNumber))
                {
                    IContainingCodeTracker containingCodeTracker = containedLines.Count > 0
                        ? this.CreateCoverageLines(textSnapshot, containedLines, codeSpanRange)
                        : this.CreateNotIncluded(textSnapshot, codeSpanRange);
                    containingCodeTrackers.Add(containingCodeTracker);

                    containedLines = new List<ILine>();
                    codeSpanRange = GetNextCodeSpanRange();
                }
            }

            return containingCodeTrackers;
        }

        #region Serialization

        private IContainingCodeTrackerTrackedLines RecreateTrackedLinesNoFileCodeSpanRangeService(
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers, 
            ITextSnapshot currentSnapshot,
            ILineExcluder lineExcluder,
            List<int> newCodeLines
        )
        {
            var containingCodeTrackers = serializedContainingCodeTrackers.Select(
                serializedContainingCodeTracker => this.RecreateCoverageLines(
                    serializedContainingCodeTracker, currentSnapshot)
            ).ToList();
            return this.containingCodeTrackedLinesFactory.Create(
                containingCodeTrackers, 
                this.GetNewCodeTrackerIfProvidesLineExcluder(lineExcluder, newCodeLines, currentSnapshot), 
                null);
        }

        private INewCodeTracker GetNewCodeTrackerIfProvidesLineExcluder(ILineExcluder lineExcluder, List<int> newCodeLines, ITextSnapshot textSnapshot)
            => lineExcluder == null ? null : this.newCodeTrackerFactory.Create(lineExcluder, newCodeLines, textSnapshot);

        private IContainingCodeTracker RecreateCoverageLines(
            SerializedContainingCodeTracker serializedContainingCodeTracker, ITextSnapshot currentSnapshot)
        {
            CodeSpanRange codeSpanRange = serializedContainingCodeTracker.CodeSpanRange;
            return serializedContainingCodeTracker.Lines[0].CoverageType == DynamicCoverageType.Dirty
                ? this.containingCodeTrackerFactory.CreateDirty(currentSnapshot, codeSpanRange, SpanTrackingMode.EdgeExclusive)
                : this.CreateCoverageLines(currentSnapshot, this.AdjustCoverageLines(serializedContainingCodeTracker.Lines), codeSpanRange);
        }

        private List<ILine> AdjustCoverageLines(List<DynamicLine> dynamicLines)
            => dynamicLines.Select(dynamicLine => new AdjustedLine(dynamicLine)).Cast<ILine>().ToList();

        private List<IContainingCodeTracker> RecreateContainingCodeTrackers(
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers,
            ITextSnapshot currentSnapshot
        ) => serializedContainingCodeTrackers.Select(
            serializedContainingCodeTracker => this.RecreateContainingCodeTracker(
                serializedContainingCodeTracker, currentSnapshot)
            ).ToList();

        private IContainingCodeTracker RecreateContainingCodeTracker(
            SerializedContainingCodeTracker serializedContainingCodeTracker, 
            ITextSnapshot currentSnapshot
        )
        {
            CodeSpanRange codeSpanRange = serializedContainingCodeTracker.CodeSpanRange;
            IContainingCodeTracker containingCodeTracker = null;
            switch (serializedContainingCodeTracker.Type)
            {
                case ContainingCodeTrackerType.OtherLines:
                    containingCodeTracker = this.CreateOtherLines(currentSnapshot, codeSpanRange);
                    break;
                case ContainingCodeTrackerType.NotIncluded:
                    containingCodeTracker = this.CreateNotIncluded(currentSnapshot, codeSpanRange);
                    break;
                case ContainingCodeTrackerType.CoverageLines:
                    containingCodeTracker = this.RecreateCoverageLines(serializedContainingCodeTracker, currentSnapshot);
                    break;
            }

            return containingCodeTracker;
        }

        private IContainingCodeTrackerTrackedLines RecreateTrackedLinesFileCodeSpanRangeService(
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers, 
            ITextSnapshot currentSnapshot,
            ICoverageContentType coverageContentType)
        {
            List<IContainingCodeTracker> containingCodeTrackers = this.RecreateContainingCodeTrackers(
                serializedContainingCodeTrackers, currentSnapshot);
            List<CodeSpanRange> codeSpanRanges = coverageContentType.FileCodeSpanRangeService.GetFileCodeSpanRanges(currentSnapshot);
            INewCodeTracker newCodeTracker = this.RecreateNewCodeTracker(
                serializedContainingCodeTrackers,
                currentSnapshot,
                coverageContentType,
                codeSpanRanges);
            return this.containingCodeTrackedLinesFactory.Create(
                containingCodeTrackers, 
                newCodeTracker, 
                this.GetFileCodeSpanRangeServiceForChanges(coverageContentType)
            );
        }

        private INewCodeTracker RecreateNewCodeTracker(
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers,
            ITextSnapshot currentSnapshot,
            ICoverageContentType coverageContentType,
            List<CodeSpanRange> codeSpanRanges
        )
        {
            if (coverageContentType.LineExcluder == null) return null;

            List<CodeSpanRange> newCodeSpanRanges = this.GetNewCodeSpanRanges(
                codeSpanRanges,
                serializedContainingCodeTrackers.Select(serializedContainingCodeTracker => serializedContainingCodeTracker.CodeSpanRange));
            IEnumerable<int> newCodeLineNumbers = this.GetRecreateNewCodeLineNumbers(newCodeSpanRanges, coverageContentType.UseFileCodeSpanRangeServiceForChanges);
            return this.newCodeTrackerFactory.Create(coverageContentType.LineExcluder, newCodeLineNumbers, currentSnapshot);
        }

        private List<CodeSpanRange> GetNewCodeSpanRanges(List<CodeSpanRange> currentCodeSpanRanges, IEnumerable<CodeSpanRange> serializedCodeSpanRanges)
        {
            foreach (CodeSpanRange serializedCodeSpanRange in serializedCodeSpanRanges)
            {
                _ = currentCodeSpanRanges.Remove(serializedCodeSpanRange);
            }

            return currentCodeSpanRanges;
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
            SerializedEditorDynamicCoverage serializedEditorDynamicCoverage = this.jsonConvertService.DeserializeObject<SerializedEditorDynamicCoverage>(serializedCoverage);
            bool usedFileCodeSpanRangeService = serializedEditorDynamicCoverage.UsedFileCodeSpanRangeService;
            IContainingCodeTrackerTrackedLines trackedLines = this.TextUnchanged(serializedEditorDynamicCoverage, currentSnapshot)
                ? this.RecreateTrackedLines(
                    serializedEditorDynamicCoverage.SerializedContainingCodeTrackers,
                    serializedEditorDynamicCoverage.NewCodeLineNumbers,
                    currentSnapshot,
                    usedFileCodeSpanRangeService
                    )
                : this.GetNonTrackingTrackedLines();
            return new ContainingCodeTrackerTrackedLinesWithState(trackedLines, usedFileCodeSpanRangeService);
        }

        private IContainingCodeTrackerTrackedLines RecreateTrackedLines(
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers, 
            List<int> newCodeLineNumbers,
            ITextSnapshot currentSnapshot,
            bool usedFileCodeSpanRangeService
        )
        {
            ICoverageContentType coverageContentType = this.GetCoverageContentType(currentSnapshot);
            return usedFileCodeSpanRangeService ? 
                this.RecreateTrackedLinesFileCodeSpanRangeService(serializedContainingCodeTrackers, currentSnapshot, coverageContentType) :
                this.RecreateTrackedLinesNoFileCodeSpanRangeService(serializedContainingCodeTrackers, currentSnapshot, coverageContentType.LineExcluder, newCodeLineNumbers);
        }

        private bool TextUnchanged(SerializedEditorDynamicCoverage serializedEditorDyamicCoverage, ITextSnapshot textSnapshot)
        {
            string previousText = serializedEditorDyamicCoverage.Text;
            string currentText = textSnapshot.GetText();
            return previousText == currentText;
        }

        public string Serialize(ITrackedLines trackedLines, string text)
        {
            var containingCodeTrackerTrackedLinesWithState = trackedLines as ContainingCodeTrackerTrackedLinesWithState;
            List<SerializedContainingCodeTracker> serializedContainingCodeTrackers = this.GetSerializedContainingCodeTrackers(containingCodeTrackerTrackedLinesWithState);
            var newCodeLineNumbers = new List<int>();
            if (containingCodeTrackerTrackedLinesWithState.NewCodeTracker != null)
            {
                newCodeLineNumbers = containingCodeTrackerTrackedLinesWithState.NewCodeTracker.Lines.Select(l => l.Number).ToList();
            }

            return this.jsonConvertService.SerializeObject(
                new SerializedEditorDynamicCoverage { 
                    SerializedContainingCodeTrackers = serializedContainingCodeTrackers, 
                    Text = text,
                    NewCodeLineNumbers = newCodeLineNumbers,
                    UsedFileCodeSpanRangeService = containingCodeTrackerTrackedLinesWithState.UsedFileCodeSpanRangeService
                });
        }

        private List<SerializedContainingCodeTracker> GetSerializedContainingCodeTrackers(IContainingCodeTrackerTrackedLines trackedLines)
            => trackedLines.ContainingCodeTrackers.Select(
                containingCodeTracker => SerializedContainingCodeTracker.From(containingCodeTracker.GetState())).ToList();

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
        #endregion
    }
}
