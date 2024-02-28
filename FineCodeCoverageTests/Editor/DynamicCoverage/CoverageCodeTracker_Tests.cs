using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CoverageCodeTracker_Tests
    {
        [Test]
        public void Should_Have_Correct_ContainingCodeTrackerType()
        {
            var autoMoqer = new AutoMoqer();
            var containingCodeTracker = autoMoqer.Create<CoverageCodeTracker>();
            Assert.That(containingCodeTracker.Type, Is.EqualTo(ContainingCodeTrackerType.CoverageLines));
        }

        [Test]
        public void Should_Return_Lines_From_TrackedCoverageLines_When_No_DirtyLine()
        {
            var autoMoqer = new AutoMoqer();
            var trackedLines = new List<IDynamicLine> { new Mock<IDynamicLine>().Object };
            autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines)
                .Returns(trackedLines);
            var containingCodeTracker = autoMoqer.Create<CoverageCodeTracker>();

            Assert.That(trackedLines, Is.SameAs(containingCodeTracker.Lines));

        }

        [TestCase(true,true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false,false)]
        public void Should_Create_The_DirtyLine_And_Be_Changed_When_Text_Changed_And_Intersected(bool textChanged, bool intersected)
        {
            var expectedCreatedDirtyLine = textChanged && intersected;
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanAndLineRanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(1, 2), 0, 1) };
            var autoMoqer = new AutoMoqer();
            var containgCodeTrackerProcessResult = new Mock<IContainingCodeTrackerProcessResult>().Object;


            var mockTrackingSpanRange = new Mock<ITrackingSpanRange>();
            var firstTrackingSpan = new Mock<ITrackingSpan>().Object;
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.GetFirstTrackingSpan()).Returns(firstTrackingSpan);

            var mockDirtyLineFactory = autoMoqer.GetMock<IDirtyLineFactory>();
            var mockDirtyLine = new Mock<ITrackingLine>();
            var dirtyDynamicLine = new Mock<IDynamicLine>().Object;
            mockDirtyLine.SetupGet(dirtyLine => dirtyLine.Line).Returns(dirtyDynamicLine);
            mockDirtyLineFactory.Setup(
                dirtyLineFactory => dirtyLineFactory.Create(firstTrackingSpan, textSnapshot)
            ).Returns(mockDirtyLine.Object);

            var coverageCodeTracker = autoMoqer.Create<CoverageCodeTracker>();

            var trackingSpanRangeProcessResult = new TrackingSpanRangeProcessResult(
                    mockTrackingSpanRange.Object,
                    intersected ? new List<SpanAndLineRange>() : newSpanAndLineRanges,
                    false,
                    textChanged
            );
            var updated = coverageCodeTracker.Update(trackingSpanRangeProcessResult, textSnapshot, newSpanAndLineRanges);

            Assert.That(updated, Is.EqualTo(expectedCreatedDirtyLine));

            mockDirtyLineFactory.Verify(
                dirtyLineFactory => dirtyLineFactory.Create(firstTrackingSpan, textSnapshot),
                MoqAssertionsHelper.ExpectedTimes(expectedCreatedDirtyLine));

            var lines = coverageCodeTracker.Lines;
            if (expectedCreatedDirtyLine)
            {
                Assert.That(lines.Single(), Is.SameAs(dirtyDynamicLine));
            }
            else
            {
                Assert.That(lines, Is.Empty);
            }
        }


        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_TrackedCoverageLines_When_Do_Not_Create_DirtyLine(bool trackedCoverageLinesChanged)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var autoMoqer = new AutoMoqer();
            var mockTrackedCoverageLines = autoMoqer.GetMock<ITrackedCoverageLines>();
            mockTrackedCoverageLines.Setup(trackedCoverageLines => trackedCoverageLines.Update(textSnapshot)).Returns(trackedCoverageLinesChanged);
            var newSpanAndLineRanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(1, 2), 0, 1) };
            var trackingSpanRangeProcessResult = new TrackingSpanRangeProcessResult(
                new Mock<ITrackingSpanRange>().Object,
                new List<SpanAndLineRange>(),
                false,
                false
            );

            var coverageCodeTracker = autoMoqer.Create<CoverageCodeTracker>();

            var updated = coverageCodeTracker.Update(trackingSpanRangeProcessResult, textSnapshot, newSpanAndLineRanges);

            Assert.That(updated, Is.EqualTo(trackedCoverageLinesChanged));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_DirtyLine_When_DirtyLine(bool dirtyLineChanged)
        {
            var textSnapshot2 = new Mock<ITextSnapshot>().Object;
            var spanAndLineRange2 = new List<SpanAndLineRange>() { new SpanAndLineRange(new Span(1, 3), 0, 0) };
            var autoMoqer = new AutoMoqer();
            var mockDirtyLineFactory = autoMoqer.GetMock<IDirtyLineFactory>();
            var mockDirtyLine = new Mock<ITrackingLine>();
            mockDirtyLine.Setup(dirtyLine => dirtyLine.Update(textSnapshot2)).Returns(dirtyLineChanged);
            mockDirtyLineFactory.Setup(dirtyLineFactory => dirtyLineFactory.Create(It.IsAny<ITrackingSpan>(), It.IsAny<ITextSnapshot>())).Returns(mockDirtyLine.Object);

            var coverageCodeTracker = autoMoqer.Create<CoverageCodeTracker>();

            var trackingSpanRangeProcessResult1 = new TrackingSpanRangeProcessResult(
                new Mock<ITrackingSpanRange>().Object,
                new List<SpanAndLineRange>(),
                false,
                true
            );
            coverageCodeTracker.Update(trackingSpanRangeProcessResult1, new Mock<ITextSnapshot>().Object, new List<SpanAndLineRange>() { new SpanAndLineRange(new Span(1, 2), 0, 0) });

            var trackingSpanRangeProcessResult2 = new TrackingSpanRangeProcessResult(
                new Mock<ITrackingSpanRange>().Object,
                new List<SpanAndLineRange>(),
                false,
                true
            );

            var updated = coverageCodeTracker.Update(trackingSpanRangeProcessResult2, textSnapshot2, spanAndLineRange2);

            Assert.That(dirtyLineChanged, Is.EqualTo(updated));
        }

    }
    

}
