using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    class Line : ILine
    {
        public Line(int number):this(number, CoverageType.Covered)
        {
        }
        public Line(int number, CoverageType coverageType)
        {
            Number = number;
            CoverageType = coverageType;
        }
        public override bool Equals(object obj)
        {
            var other = obj as ILine;
            return other.Number == Number && other.CoverageType == CoverageType;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = 1698846147;
            hashCode = hashCode * -1521134295 + Number.GetHashCode();
            hashCode = hashCode * -1521134295 + CoverageType.GetHashCode();
            return hashCode;
        }

        public int Number { get; }

        public CoverageType CoverageType { get; }
    }

    internal static class TestHelper
    {
        public static CodeSpanRange CodeSpanRangeFromLine(ILine line)
        {
            return CodeSpanRange.SingleLine(line.Number - 1);
        }
    }
    
    internal class ContainingCodeTrackedLinesBuilder_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Have_NewCodeTracker_When_CoverageContentType_Has_LineExcluder(bool hasLineExcluder)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName).Returns("contenttypename");
            
            var autoMoqer = new AutoMoqer();
            var lineExcluder = new Mock<ILineExcluder>().Object;
            var newCodeTracker = new Mock<INewCodeTracker>().Object;
            var mockNewCodeTrackerFactory = autoMoqer.GetMock<INewCodeTrackerFactory>();
            mockNewCodeTrackerFactory.Setup(newCodeTrackerFactory => newCodeTrackerFactory.Create(lineExcluder)).Returns(newCodeTracker);

            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var trackedLinesFromFactory = new Mock<IContainingCodeTrackerTrackedLines>().Object;

            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                It.IsAny<List<IContainingCodeTracker>>(),
                hasLineExcluder ? newCodeTracker : null,
                It.IsAny<IFileCodeSpanRangeService>()
                )).Returns(trackedLinesFromFactory);
            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.Setup(coverageContentType => coverageContentType.ContentTypeName).Returns("contenttypename");
            if(hasLineExcluder)
            {
                mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.LineExcluder).Returns(lineExcluder);
            }
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var trackedLines = containingCodeTrackedLinesBuilder.Create(new List<ILine> {}, mockTextSnapshot.Object);

            Assert.That(trackedLines, Is.SameAs(trackedLinesFromFactory));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Use_CoverageContentType_FileCodeSpanRangeService_When_UseFileCodeSpanRangeServiceForChanges(bool useFileCodeSpanRangeServiceForChanges)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName).Returns("contenttypename");

            var autoMoqer = new AutoMoqer();
            var fileCodeSpanRangeService = new Mock<IFileCodeSpanRangeService>().Object;
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var trackedLinesFromFactory = new TrackedLines(null, null, null);
            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                It.IsAny<List<IContainingCodeTracker>>(),
                It.IsAny<INewCodeTracker>(),
                useFileCodeSpanRangeServiceForChanges ? fileCodeSpanRangeService : null
                )).Returns(trackedLinesFromFactory);

            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.UseFileCodeSpanRangeServiceForChanges).Returns(useFileCodeSpanRangeServiceForChanges);
            mockCoverageContentType.Setup(coverageContentType => coverageContentType.ContentTypeName).Returns("contenttypename");
            
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.FileCodeSpanRangeService).Returns(fileCodeSpanRangeService);
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var trackedLines = containingCodeTrackedLinesBuilder.Create(new List<ILine> { }, mockTextSnapshot.Object);

            Assert.That(trackedLines, Is.SameAs(trackedLinesFromFactory));
        }

        [TestCase(ContainingCodeTrackerType.CoverageLines, 1, DynamicCoverageType.Covered)]
        [TestCase(ContainingCodeTrackerType.NotIncluded, 1, DynamicCoverageType.NotIncluded)]
        public void Should_Serialize_State_From_TrackedLines_ContainingCodeTrackers(
            ContainingCodeTrackerType containingCodeTrackerType, int lineNumber, DynamicCoverageType coverageType
        )
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance(new ICoverageContentType[0]);
            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.SerializeObject(It.IsAny<object>())).Returns("SerializedState");

            var mockContainingCodeTracker = new Mock<IContainingCodeTracker>();
            var codeSpanRange = new CodeSpanRange(1, 2);
            var containingCodeTrackerState = new ContainingCodeTrackerState(containingCodeTrackerType, codeSpanRange, new List<IDynamicLine> { new DynamicLine(lineNumber, coverageType) });
            mockContainingCodeTracker.Setup(containingCodeTracker => containingCodeTracker.GetState()).Returns(containingCodeTrackerState);
            var containingCodeTrackers = new List<IContainingCodeTracker> {
                        mockContainingCodeTracker.Object,
                    };

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            var mockContainingCodeTrackerTrackedLines = new Mock<IContainingCodeTrackerTrackedLines>();
            mockContainingCodeTrackerTrackedLines.SetupGet(containingCodeTrackerTrackedLines => containingCodeTrackerTrackedLines.ContainingCodeTrackers).Returns(containingCodeTrackers);
            
            var serialized = containingCodeTrackedLinesBuilder.Serialize(
                mockContainingCodeTrackerTrackedLines.Object);

            Assert.That("SerializedState", Is.EqualTo(serialized));

            var serializedState = mockJsonConvertService.Invocations.GetMethodInvocationSingleArgument<List<SerializedState>>(
                nameof(IJsonConvertService.SerializeObject)).Single().Single();

            Assert.That(serializedState.Type, Is.EqualTo(containingCodeTrackerType));
            Assert.That(serializedState.CodeSpanRange, Is.SameAs(codeSpanRange));
            var serializedLine = serializedState.Lines.Single();
            Assert.That(serializedLine.Number, Is.EqualTo(lineNumber));
            Assert.That(serializedLine.CoverageType, Is.EqualTo(coverageType));
        }
    }

    internal class ContainingCodeTrackedLinesBuilder_ContentType_FileLineCoverageService_Tests
    {
        [Test]
        public void Should_Create_CoverageLines_ContainingCodeTracker_For_Each_Line_When_Null_CodeSpanRanges()
        {
            var line1 = new Line(1);
            var line2 = new Line(2);

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName).Returns("contenttypename");

            var autoMoqer = new AutoMoqer();
            var mockCodeSpanRangeContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            var containingCodeTracker1 = new Mock<IContainingCodeTracker>().Object;
            var containingCodeTracker2 = new Mock<IContainingCodeTracker>().Object;
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                    mockTextSnapshot.Object, new List<ILine> { line1 }, TestHelper.CodeSpanRangeFromLine(line1), SpanTrackingMode.EdgeExclusive)
                ).Returns(containingCodeTracker1);
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
               codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                   mockTextSnapshot.Object, new List<ILine> { line2 }, TestHelper.CodeSpanRangeFromLine(line2), SpanTrackingMode.EdgeExclusive)
               ).Returns(containingCodeTracker2);
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var trackedLinesFromFactory = new TrackedLines(null, null, null);

            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                new List<IContainingCodeTracker> { containingCodeTracker1, containingCodeTracker2 },
                It.IsAny<INewCodeTracker>(),
                It.IsAny<IFileCodeSpanRangeService>()
                )).Returns(trackedLinesFromFactory);
            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.ContentTypeName).Returns("contenttypename");
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.FileCodeSpanRangeService).Returns(new Mock<IFileCodeSpanRangeService>().Object);
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var trackedLines = containingCodeTrackedLinesBuilder.Create(new List<ILine> { line1, line2 }, mockTextSnapshot.Object);

            Assert.That(trackedLines, Is.SameAs(trackedLinesFromFactory));
        }
    
        private struct OtherLineText
        {
            public int LineNumber { get; set; }
            public string Text { get; set; }
        }

        private class DummyFileCodeSpanRangeService : IFileCodeSpanRangeService
        {
            private readonly ITextSnapshot expectedSnapshot;
            private readonly List<CodeSpanRange> codeSpanRanges;

            public DummyFileCodeSpanRangeService(ITextSnapshot expectedSnapshot, List<CodeSpanRange> codeSpanRanges)
            {
                this.expectedSnapshot = expectedSnapshot;
                this.codeSpanRanges = codeSpanRanges;
            }
            public List<CodeSpanRange> GetFileCodeSpanRanges(ITextSnapshot snapshot)
            {
                Assert.That(snapshot, Is.SameAs(expectedSnapshot));
                return codeSpanRanges;
            }
        }

        private void TestCreatesOrderedContainingCodeTrackers(
            List<ILine> lines,
            bool coverageOnlyFromFileCodeSpanRangeService,
            List<CodeSpanRange> codeSpanRanges,
            int textSnapshotLineCount,
            Action<ITextSnapshot> textSnapshotCallback,
            Action<Mock<ICodeSpanRangeContainingCodeTrackerFactory>> setUpCodeSpanRangeContainingCodeTrackerFactory,
            List<OtherLineText> otherLineTexts,
            List<IContainingCodeTracker> expectedOrderedContainingCodeTrackers
            )
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(textSnapshotLineCount);
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName).Returns("contenttypename");
            textSnapshotCallback(mockTextSnapshot.Object);

            var autoMoqer = new AutoMoqer();
            var mockTextSnapshotText = autoMoqer.GetMock<ITextSnapshotText>(MockBehavior.Strict);
            otherLineTexts.ForEach(
                otherLineText => mockTextSnapshotText.Setup(
                    textSnapshotText => textSnapshotText.GetLineText(mockTextSnapshot.Object, otherLineText.LineNumber)
                ).Returns(otherLineText.Text));
            var mockCodeSpanRangeContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            setUpCodeSpanRangeContainingCodeTrackerFactory(mockCodeSpanRangeContainingCodeTrackerFactory);

            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var trackedLinesFromFactory = new TrackedLines(null, null, null);
            mockContainingCodeTrackedLinesFactory.Setup(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    expectedOrderedContainingCodeTrackers,
                    It.IsAny<INewCodeTracker>(),
                    It.IsAny<IFileCodeSpanRangeService>()
                )).Returns(trackedLinesFromFactory);

            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.ContentTypeName).Returns("contenttypename");
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.CoverageOnlyFromFileCodeSpanRangeService).Returns(coverageOnlyFromFileCodeSpanRangeService);
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.FileCodeSpanRangeService).Returns(
                new DummyFileCodeSpanRangeService(mockTextSnapshot.Object,codeSpanRanges));
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();

            var trackedLines = containingCodeTrackedLinesBuilder.Create(lines, mockTextSnapshot.Object);

            Assert.That(trackedLines, Is.SameAs(trackedLinesFromFactory));
        }

        private IContainingCodeTracker CreateContainingCodeTracker(CodeSpanRange codeSpanRangeForOrdering)
        {
            var mockContainingCodeTracker = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker.Setup(cct => cct.GetState()).Returns(
                new ContainingCodeTrackerState(ContainingCodeTrackerType.CoverageLines, codeSpanRangeForOrdering, null)
            );
            return mockContainingCodeTracker.Object;
        }

        [Test]
        public void Should_Create_CoverageLinesTracker_For_Adjusted_Lines_In_CodeSpanRange()
        {
            var coverageLinesTracker = CreateContainingCodeTracker(new CodeSpanRange(0, 1));

            var line1 = new Line(1);
            var line2 = new Line(2);
            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { line1, line2 },
                false,
                new List<CodeSpanRange> { new CodeSpanRange(0,1) },
                2,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                        
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { line1, line2},
                            new CodeSpanRange(0, 1),
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker);
                },
                new List<OtherLineText> { },
                new List<IContainingCodeTracker> { coverageLinesTracker}
                );
        }

        [Test]
        public void Should_Create_NotIncludedTracker_For_CodeSpanRange_With_No_Coverage()
        {
            var notIncludedTracker = CreateContainingCodeTracker(new CodeSpanRange(0, 3));

            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { },
                false,
                new List<CodeSpanRange> { new CodeSpanRange(0, 3) },
                3,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateNotIncluded(
                            textSnapshotForSetup,
                            new CodeSpanRange(0, 3),
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(notIncludedTracker);
                },
                new List<OtherLineText> { },
                new List<IContainingCodeTracker> { notIncludedTracker }
                );
        }

        [Test]
        public void Should_Create_OtherLinesTracker_For_Lines_Between_CodeSpanRanges_That_Are_Not_Whitespace()
        {
            var coverageLinesRange1 = new CodeSpanRange(0, 1);
            var coverageLinesTracker = CreateContainingCodeTracker(coverageLinesRange1);
            var otherLineRange = new CodeSpanRange(2, 2);
            var otherLineTracker = CreateContainingCodeTracker(otherLineRange);
            var coverageLinesRange2 = new CodeSpanRange(3, 4);
            var coverageLinesTracker2 = CreateContainingCodeTracker(coverageLinesRange2);

            var range1Line = new Line(1);
            var range2Line = new Line(4);
            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { range1Line, range2Line },
                false,
                new List<CodeSpanRange> { coverageLinesRange1, coverageLinesRange2 },
                4,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { range1Line },
                            coverageLinesRange1,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateOtherLines(
                            textSnapshotForSetup,
                            otherLineRange,
                            SpanTrackingMode.EdgeNegative)
                            ).Returns(otherLineTracker);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { range2Line},
                            coverageLinesRange2,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker2);
                },
                new List<OtherLineText> {
                    new OtherLineText { LineNumber = 2, Text = "text" },
                },
                new List<IContainingCodeTracker> { coverageLinesTracker, otherLineTracker, coverageLinesTracker2 }
                );
        }

        [Test]
        public void Should_Create_OtherLinesTracker_For_Each_Line_After_Last_CodeSpanRange_That_Is_Not_Whitespace()
        {
            var coverageLinesTracker = CreateContainingCodeTracker(new CodeSpanRange(0, 1));
            var otherLineTracker = CreateContainingCodeTracker(CodeSpanRange.SingleLine(2));

            var line1 = new Line(1);
            var line2 = new Line(2);
            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { line1, line2 },
                false,
                new List<CodeSpanRange> { new CodeSpanRange(0, 1) },
                4,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { line1, line2 },
                            new CodeSpanRange(0, 1),
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateOtherLines(
                            textSnapshotForSetup,
                            CodeSpanRange.SingleLine(2),
                            SpanTrackingMode.EdgeNegative)
                            ).Returns(otherLineTracker);

                },
                new List<OtherLineText> { 
                    new OtherLineText { LineNumber = 2, Text = "text" },
                    new OtherLineText { LineNumber = 3, Text = "    " }
                },
                new List<IContainingCodeTracker> { coverageLinesTracker, otherLineTracker }
                );
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_CoverageLinesTracker_For_Each_CoverageLine_Not_In_CodeSpanRange_If_CoverageOnlyFromFileCodeSpanRangeService_Is_False(
            bool coverageOnlyFromFileCodeSpanRangeService
        )
        {
            // before first CodeSpanRange
            var coverageLineNotInRangeRange1 = new CodeSpanRange(0, 0);
            var notInRangeCoverageLineTracker1 = CreateContainingCodeTracker(coverageLineNotInRangeRange1);

            var coverageLinesRange1 = new CodeSpanRange(1, 1);
            var coverageLinesTracker = CreateContainingCodeTracker(coverageLinesRange1);

            // in between CodeSpanRange2
            var coverageLineNotInRangeRange2 = new CodeSpanRange(2, 2);
            var notInRangeCoverageLineTracker2 = CreateContainingCodeTracker(coverageLineNotInRangeRange2);
            
            var coverageLinesRange2 = new CodeSpanRange(3, 3);
            var coverageLinesTracker2 = CreateContainingCodeTracker(coverageLinesRange2);

            // after last CodeSpanRange
            var coverageLineNotInRangeRange3 = new CodeSpanRange(4, 4);
            var notInRangeCoverageLineTracker3 = CreateContainingCodeTracker(coverageLineNotInRangeRange3);

            var notInRangeLine1 = new Line(1);
            var range1Line = new Line(2);
            var notInRangeLine2 = new Line(3);
            var range2Line = new Line(4);
            var notInRangeLine3 = new Line(5);
            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { notInRangeLine1, range1Line, notInRangeLine2, range2Line, notInRangeLine3 },
                coverageOnlyFromFileCodeSpanRangeService,
                new List<CodeSpanRange> { coverageLinesRange1, coverageLinesRange2 },
                4,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { notInRangeLine1},
                            coverageLineNotInRangeRange1,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(notInRangeCoverageLineTracker1);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { range1Line },
                            coverageLinesRange1,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { notInRangeLine2},
                            coverageLineNotInRangeRange2,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(notInRangeCoverageLineTracker2);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { range2Line },
                            coverageLinesRange2,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker2);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { notInRangeLine3 },
                            coverageLineNotInRangeRange3,
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(notInRangeCoverageLineTracker3);
                },
                new List<OtherLineText> {},
                coverageOnlyFromFileCodeSpanRangeService ? 
                    new List<IContainingCodeTracker> { coverageLinesTracker, coverageLinesTracker2 } : 
                    new List<IContainingCodeTracker> { 
                        notInRangeCoverageLineTracker1, 
                        coverageLinesTracker, 
                        notInRangeCoverageLineTracker2, 
                        coverageLinesTracker2,
                        notInRangeCoverageLineTracker3
                    }
                );
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_ContainingCodeTrackers_In_CodeRange_Order(bool firstFirst)
        {
            var firstSortingRange = CodeSpanRange.SingleLine(1);
            var secondSortingRange = CodeSpanRange.SingleLine(2);
            var coverageLinesTracker = CreateContainingCodeTracker(firstFirst ? firstSortingRange : secondSortingRange);
            var otherLineTracker = CreateContainingCodeTracker(!firstFirst ? firstSortingRange : secondSortingRange);
            var expectedOrder = firstFirst ? 
                new List<IContainingCodeTracker> { coverageLinesTracker, otherLineTracker } : 
                new List<IContainingCodeTracker> { otherLineTracker, coverageLinesTracker };
            var line1 = new Line(1);
            var line2 = new Line(2);
            ITextSnapshot textSnapshotForSetup = null;
            TestCreatesOrderedContainingCodeTrackers(
                new List<ILine> { line1, line2 },
                false,
                new List<CodeSpanRange> { new CodeSpanRange(0, 1) },
                4,
                textSnapshot => textSnapshotForSetup = textSnapshot,
                mockCodeSpanRangeContainingCodeTrackerFactory =>
                {
                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                            textSnapshotForSetup,
                            new List<ILine> { line1, line2 },
                            new CodeSpanRange(0, 1),
                            SpanTrackingMode.EdgeExclusive)
                            ).Returns(coverageLinesTracker);

                    mockCodeSpanRangeContainingCodeTrackerFactory.Setup(

                        codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateOtherLines(
                            textSnapshotForSetup,
                            CodeSpanRange.SingleLine(2),
                            SpanTrackingMode.EdgeNegative)
                            ).Returns(otherLineTracker);

                },
                new List<OtherLineText> {
                    new OtherLineText { LineNumber = 2, Text = "text" },
                    new OtherLineText { LineNumber = 3, Text = "    " }
                },
                expectedOrder
                );
        }

    }

    internal class ContainingCodeTrackedLinesBuilder_ContentType_No_FileLineCoverageService_Tests
    {
        [Test]
        public void Should_Create_CoverageLines_ContainingCodeTracker_For_Each_Line()
        {
            var line1 = new Line(1);
            var line2 = new Line(2);

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName).Returns("contenttypename");

            var autoMoqer = new AutoMoqer();
            var mockCodeSpanRangeContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            var containingCodeTracker1 = new Mock<IContainingCodeTracker>().Object;
            var containingCodeTracker2 = new Mock<IContainingCodeTracker>().Object;
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                    mockTextSnapshot.Object, new List<ILine> { line1 }, TestHelper.CodeSpanRangeFromLine(line1), SpanTrackingMode.EdgeExclusive)
                ).Returns(containingCodeTracker1);
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
               codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                   mockTextSnapshot.Object, new List<ILine> { line2 }, TestHelper.CodeSpanRangeFromLine(line2), SpanTrackingMode.EdgeExclusive)
               ).Returns(containingCodeTracker2);
            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var trackedLinesFromFactory = new TrackedLines(null, null, null);

            mockContainingCodeTrackedLinesFactory.Setup(containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                new List<IContainingCodeTracker> { containingCodeTracker1, containingCodeTracker2 },
                It.IsAny<INewCodeTracker>(),
                It.IsAny<IFileCodeSpanRangeService>()
                )).Returns(trackedLinesFromFactory);
            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.ContentTypeName).Returns("contenttypename");
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType .Object});

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();
            
            var trackedLines = containingCodeTrackedLinesBuilder.Create(new List<ILine> { line1, line2 }, mockTextSnapshot.Object);
            
            Assert.That(trackedLines, Is.SameAs(trackedLinesFromFactory));
        }

        [TestCase(true)]
        [TestCase(false)]
        [TestCase(false, DynamicCoverageType.NotCovered, CoverageType.NotCovered)]
        [TestCase(false, DynamicCoverageType.Partial, CoverageType.Partial)]
        public void Should_Create_ContainingCodeTrackers_From_Serialized_State_If_Contained_In_Snapshot(
            bool containedInSnapshot,
            DynamicCoverageType dynamicCoverageType = DynamicCoverageType.Covered,
            CoverageType expectedAdjustedCoverageType = CoverageType.Covered
            )
        {
            var autoMoqer = new AutoMoqer();

            var coverageCodeSpanRange = new CodeSpanRange(0, 0);
            var dirtyCodeSpanRange = new CodeSpanRange(1, 1);

            var mockTextSnaphot = new Mock<ITextSnapshot>();
            mockTextSnaphot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName)
                .Returns("contenttypename");
            mockTextSnaphot.SetupGet(textSnapshot => textSnapshot.LineCount)
                .Returns(containedInSnapshot ? 2 : 1);

            var coverageContainingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            var dirtyContainingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            var createdContainingCodeTrackers = new List<IContainingCodeTracker> { coverageContainingCodeTracker, dirtyContainingCodeTracker };
            if (!containedInSnapshot)
            {
                createdContainingCodeTrackers.RemoveAt(1);
            }
            var mockCodeSpanRangeContainingCodeTrackerFactory = autoMoqer.GetMock<ICodeSpanRangeContainingCodeTrackerFactory>();
            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(
                    mockTextSnaphot.Object,
                    // adjusted IDynamicLine
                    new List<ILine>{ new Line(1, expectedAdjustedCoverageType) },
                    coverageCodeSpanRange,
                    SpanTrackingMode.EdgeExclusive
                )).Returns(coverageContainingCodeTracker);

            mockCodeSpanRangeContainingCodeTrackerFactory.Setup(
                codeSpanRangeContainingCodeTrackerFactory => codeSpanRangeContainingCodeTrackerFactory.CreateDirty(
                    mockTextSnaphot.Object,
                    dirtyCodeSpanRange,
                    SpanTrackingMode.EdgeExclusive
                )).Returns(dirtyContainingCodeTracker);

            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var containingCodeTrackerTrackedLinesFromFactory = new Mock<IContainingCodeTrackerTrackedLines>().Object;
            mockContainingCodeTrackedLinesFactory.Setup(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    createdContainingCodeTrackers,
                    It.IsAny<INewCodeTracker>(),
                    null
                )).Returns(containingCodeTrackerTrackedLinesFromFactory);
            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.ContentTypeName)
                .Returns("contenttypename");
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object});

            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns(
                    new List<SerializedState>
                    {
                        new SerializedState(coverageCodeSpanRange, ContainingCodeTrackerType.CoverageLines, new List<DynamicLine>
                        {
                            new DynamicLine(0, dynamicCoverageType)
                        }),
                         new SerializedState(dirtyCodeSpanRange, ContainingCodeTrackerType.CoverageLines, new List<DynamicLine>
                        {
                            new DynamicLine(1, DynamicCoverageType.Dirty)
                        })
                    }
                );
                
            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();


            var containingCodeTrackerTrackedLines = containingCodeTrackedLinesBuilder.Create("serializedState", mockTextSnaphot.Object);

            Assert.That(containingCodeTrackerTrackedLines, Is.SameAs(containingCodeTrackerTrackedLinesFromFactory));

        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Use_NewCodeTracker_With_Line_Excluder_If_CoverageContentTypeProvides(bool hasLineExcluder)
        {
            var autoMoqer = new AutoMoqer();
            
            var mockTextSnaphot = new Mock<ITextSnapshot>();
            mockTextSnaphot.SetupGet(textSnapshot => textSnapshot.ContentType.TypeName)
                .Returns("contenttypename");

            var newLineExcluder = new Mock<ILineExcluder>().Object;
            var mockCoverageContentType = new Mock<ICoverageContentType>();
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.ContentTypeName)
                .Returns("contenttypename");
            mockCoverageContentType.SetupGet(coverageContentType => coverageContentType.LineExcluder).Returns(hasLineExcluder ? newLineExcluder : null);
            autoMoqer.SetInstance(new ICoverageContentType[] { mockCoverageContentType.Object });

            var newCodeTracker = new Mock<INewCodeTracker>().Object;
            var mockNewCodeTrackerFactory = autoMoqer.GetMock<INewCodeTrackerFactory>();
            mockNewCodeTrackerFactory.Setup(newCodeTrackeFactory => newCodeTrackeFactory.Create(newLineExcluder))
                .Returns(newCodeTracker);

            var mockContainingCodeTrackedLinesFactory = autoMoqer.GetMock<IContainingCodeTrackedLinesFactory>();
            var containingCodeTrackerTrackedLinesFromFactory = new Mock<IContainingCodeTrackerTrackedLines>().Object;
            mockContainingCodeTrackedLinesFactory.Setup(
                containingCodeTrackedLinesFactory => containingCodeTrackedLinesFactory.Create(
                    It.IsAny<List<IContainingCodeTracker>>(),
                    hasLineExcluder ? newCodeTracker : null,
                    null
                )).Returns(containingCodeTrackerTrackedLinesFromFactory);
            
            var mockJsonConvertService = autoMoqer.GetMock<IJsonConvertService>();
            mockJsonConvertService.Setup(jsonConvertService => jsonConvertService.DeserializeObject<List<SerializedState>>("serializedState"))
                .Returns( new List<SerializedState> { });

            var containingCodeTrackedLinesBuilder = autoMoqer.Create<ContainingCodeTrackedLinesBuilder>();


            var containingCodeTrackerTrackedLines = containingCodeTrackedLinesBuilder.Create("serializedState", mockTextSnaphot.Object);

            Assert.That(containingCodeTrackerTrackedLines, Is.SameAs(containingCodeTrackerTrackedLinesFromFactory));

        }
    }

}
