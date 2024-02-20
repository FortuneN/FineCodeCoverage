using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class LinesContainingCodeTrackerFactory_Tests
    {
        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_For_A_Line_Create_IContainingCodeTracker_From_TrackedCoverageLines(SpanTrackingMode spanTrackingMode)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.Number).Returns(5);
            var adjustedLine = 4;

            var autoMoqer = new AutoMoqer();
            var trackingSpan = new Mock<ITrackingSpan>().Object;
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, adjustedLine,spanTrackingMode))
                .Returns(trackingSpan);
            var coverageLine = new Mock<ICoverageLine>().Object;
            autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan, mockLine.Object))
                 .Returns(coverageLine);
            var trackedCoverageLines = new Mock<ITrackedCoverageLines>().Object;
            autoMoqer.Setup<ITrackedCoverageLinesFactory, ITrackedCoverageLines>(
                trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(new List<ICoverageLine> { coverageLine }))
                .Returns(trackedCoverageLines);
            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            autoMoqer.Setup<ITrackedContainingCodeTrackerFactory, IContainingCodeTracker>(
                trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.Create(trackedCoverageLines))
                .Returns(containingCodeTracker);

            var linesContainingCodeTrackerFactory = autoMoqer.Create<LinesContainingCodeTrackerFactory>();

            Assert.That(linesContainingCodeTrackerFactory.Create(textSnapshot, mockLine.Object,spanTrackingMode), Is.SameAs(containingCodeTracker));
        }

        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_For_A_CodeRange_Create_IContainingCodeTracker_From_TrackedCoverageLines_And_TrackingSpanRange(SpanTrackingMode spanTrackingMode)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.Number).Returns(5);
            var mockLine2 = new Mock<ILine>();
            mockLine2.SetupGet(line => line.Number).Returns(6);

            var autoMoqer = new AutoMoqer();
            var trackingSpan = new Mock<ITrackingSpan>().Object;
            var trackingSpan2 = new Mock<ITrackingSpan>().Object;

            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, 4, spanTrackingMode))
                .Returns(trackingSpan);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, 5, spanTrackingMode))
                .Returns(trackingSpan2);

            // for the CodeSpanRange no line adjustments - CodeSpanRange in reality will contain the lines
            var codeRangeTrackingSpan20 = new Mock<ITrackingSpan>().Object;
            var codeRangeTrackingSpan22 = new Mock<ITrackingSpan>().Object;
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, 20, spanTrackingMode))
                .Returns(codeRangeTrackingSpan20);
            autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.CreateTrackingSpan(textSnapshot, 22, spanTrackingMode))
                .Returns(codeRangeTrackingSpan22);
            var trackingSpanRange = new Mock<ITrackingSpanRange>().Object;
            autoMoqer.Setup<ITrackingSpanRangeFactory, ITrackingSpanRange>(
                trackingSpanRangeFactory => trackingSpanRangeFactory.Create(
                    codeRangeTrackingSpan20,codeRangeTrackingSpan22,textSnapshot
                )).Returns(trackingSpanRange);


            var coverageLine = new Mock<ICoverageLine>().Object;
            var coverageLine2 = new Mock<ICoverageLine>().Object;
            autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan, mockLine.Object))
                 .Returns(coverageLine);
            autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan2, mockLine2.Object))
                 .Returns(coverageLine2);
            var trackedCoverageLines = new Mock<ITrackedCoverageLines>().Object;
            autoMoqer.Setup<ITrackedCoverageLinesFactory, ITrackedCoverageLines>(
                trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(new List<ICoverageLine> { coverageLine, coverageLine2 }))
                .Returns(trackedCoverageLines);



            var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            autoMoqer.Setup<ITrackedContainingCodeTrackerFactory, IContainingCodeTracker>(
                trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.Create(trackingSpanRange, trackedCoverageLines))
                .Returns(containingCodeTracker);

            var linesContainingCodeTrackerFactory = autoMoqer.Create<LinesContainingCodeTrackerFactory>();

            Assert.That(linesContainingCodeTrackerFactory.Create(
                textSnapshot, new List<ILine> { mockLine.Object, mockLine2.Object }, new CodeSpanRange(20, 22), spanTrackingMode), Is.SameAs(containingCodeTracker));
        }
    }
}
