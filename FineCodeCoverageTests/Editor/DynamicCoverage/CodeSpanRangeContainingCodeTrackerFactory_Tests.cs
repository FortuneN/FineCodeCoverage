using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CodeSpanRangeContainingCodeTrackerFactory_Tests
    {
        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_CreateCoverageLines_From_TrackedCoverageLines_And_TrackingSpanRange(SpanTrackingMode spanTrackingMode)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.Number).Returns(5);
            var adjustedLine = 4;
            var mockLine2 = new Mock<ILine>();
            mockLine2.SetupGet(line => line.Number).Returns(6);
            var adjustedLine2 = 5;
            var codeSpanRange = new CodeSpanRange(1, 10);

            var autoMoqer = new AutoMoqer();
            var trackingSpanStart = new Mock<ITrackingSpan>().Object;
            var trackingSpanEnd = new Mock<ITrackingSpan>().Object;
            var trackingSpanLine = new Mock<ITrackingSpan>().Object;
            var trackingSpanLine2 = new Mock<ITrackingSpan>().Object;
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.StartLine, spanTrackingMode))
                .Returns(trackingSpanStart);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.EndLine, spanTrackingMode))
                .Returns(trackingSpanEnd);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, adjustedLine, spanTrackingMode))
                .Returns(trackingSpanLine);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, adjustedLine2, spanTrackingMode))
                .Returns(trackingSpanLine2);


            var coverageLine = new Mock<ICoverageLine>().Object;
            var coverageLine2 = new Mock<ICoverageLine>().Object;
            autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpanLine, mockLine.Object))
                 .Returns(coverageLine);
            autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpanLine2, mockLine2.Object))
                 .Returns(coverageLine2);
            var trackedCoverageLines = new Mock<ITrackedCoverageLines>().Object;
            autoMoqer.Setup<ITrackedCoverageLinesFactory, ITrackedCoverageLines>(
                trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(new List<ICoverageLine> { coverageLine, coverageLine2 }))
                .Returns(trackedCoverageLines);

            var trackingSpanRange = new Mock<ITrackingSpanRange>().Object;
            autoMoqer.Setup<ITrackingSpanRangeFactory, ITrackingSpanRange>(
                               trackingSpanRangeFactory => trackingSpanRangeFactory.Create(trackingSpanStart, trackingSpanEnd, textSnapshot))
                .Returns(trackingSpanRange);

            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            autoMoqer.Setup<ITrackingSpanRangeContainingCodeTrackerFactory, IContainingCodeTracker>(
                trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.CreateCoverageLines(trackingSpanRange,trackedCoverageLines))
                .Returns(containingCodeTracker);

            var codeSpanRangeContainingCodeTrackerFactory = autoMoqer.Create<CodeSpanRangeContainingCodeTrackerFactory>();

            Assert.That(codeSpanRangeContainingCodeTrackerFactory.CreateCoverageLines(textSnapshot, new List<ILine> { mockLine.Object, mockLine2.Object },codeSpanRange, spanTrackingMode), Is.SameAs(containingCodeTracker));
        }

        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_CreateOtherLines_From_TrackingSpanRange(SpanTrackingMode spanTrackingMode)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var codeSpanRange = new CodeSpanRange(1, 10);
            
            var autoMoqer = new AutoMoqer();
            
            var trackingSpanStart = new Mock<ITrackingSpan>().Object;
            var trackingSpanEnd = new Mock<ITrackingSpan>().Object;
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.StartLine, spanTrackingMode))
                .Returns(trackingSpanStart);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.EndLine, spanTrackingMode))
                .Returns(trackingSpanEnd);

            var trackingSpanRange = new Mock<ITrackingSpanRange>().Object;
            autoMoqer.Setup<ITrackingSpanRangeFactory, ITrackingSpanRange>(
                               trackingSpanRangeFactory => trackingSpanRangeFactory.Create(trackingSpanStart, trackingSpanEnd, textSnapshot))
                .Returns(trackingSpanRange);

            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            autoMoqer.Setup<ITrackingSpanRangeContainingCodeTrackerFactory, IContainingCodeTracker>(
                trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.CreateOtherLines(trackingSpanRange))
                .Returns(containingCodeTracker);

            var codeSpanRangeContainingCodeTrackerFactory = autoMoqer.Create<CodeSpanRangeContainingCodeTrackerFactory>();

            Assert.That(codeSpanRangeContainingCodeTrackerFactory.CreateOtherLines(textSnapshot, codeSpanRange, spanTrackingMode), Is.SameAs(containingCodeTracker));
        }

        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_Create_NotIncluded_From_TrackingSpanRange_And_NotIncluded_TrackingLine(SpanTrackingMode spanTrackingMode)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var codeSpanRange = new CodeSpanRange(1, 10);

            var autoMoqer = new AutoMoqer();

            var trackingSpanStart = new Mock<ITrackingSpan>().Object;
            var trackingSpanEnd = new Mock<ITrackingSpan>().Object;
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.StartLine, spanTrackingMode))
                .Returns(trackingSpanStart);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, codeSpanRange.EndLine, spanTrackingMode))
                .Returns(trackingSpanEnd);

            var firstTrackingSpan = new Mock<ITrackingSpan>().Object;
            var mockTrackingSpanRange = new Mock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.GetFirstTrackingSpan()).Returns(firstTrackingSpan);
            autoMoqer.Setup<ITrackingSpanRangeFactory, ITrackingSpanRange>(
                               trackingSpanRangeFactory => trackingSpanRangeFactory.Create(trackingSpanStart, trackingSpanEnd, textSnapshot))
                .Returns(mockTrackingSpanRange.Object);

            var trackingLine = new Mock<ITrackingLine>().Object;
            autoMoqer.Setup<INotIncludedLineFactory, ITrackingLine>(notIncludedLineFactory => notIncludedLineFactory.Create(firstTrackingSpan,textSnapshot))
                .Returns(trackingLine);

            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            autoMoqer.Setup<ITrackingSpanRangeContainingCodeTrackerFactory, IContainingCodeTracker>(
                trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.CreateNotIncluded(trackingLine,mockTrackingSpanRange.Object))
                .Returns(containingCodeTracker);

            var codeSpanRangeContainingCodeTrackerFactory = autoMoqer.Create<CodeSpanRangeContainingCodeTrackerFactory>();

            Assert.That(codeSpanRangeContainingCodeTrackerFactory.CreateNotIncluded(textSnapshot, codeSpanRange, spanTrackingMode), Is.SameAs(containingCodeTracker));
        }
    }
}
