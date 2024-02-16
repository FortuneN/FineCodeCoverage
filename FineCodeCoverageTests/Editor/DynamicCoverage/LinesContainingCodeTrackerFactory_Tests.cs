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
        [Test]
        public void Should_For_A_Line_Should_Create_IContainingCodeTracker_From_TrackedCoverageLines()
        {
            throw new System.NotImplementedException();
            //var textSnapshot = new Mock<ITextSnapshot>().Object;
            //var mockLine = new Mock<ILine>();
            //mockLine.SetupGet(line => line.Number).Returns(5);
            //var adjustedLine = 4;

            //var autoMoqer = new AutoMoqer();
            //var trackingSpan = new Mock<ITrackingSpan>().Object;
            //autoMoqer.Setup<ITrackingLineFactory,ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, adjustedLine))
            //    .Returns(trackingSpan);
            //var coverageLine = new Mock<ICoverageLine>().Object;
            //autoMoqer.Setup<ICoverageLineFactory,ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan,mockLine.Object))
            //     .Returns(coverageLine);
            //var trackedCoverageLines = new Mock<ITrackedCoverageLines>().Object;
            //autoMoqer.Setup<ITrackedCoverageLinesFactory,ITrackedCoverageLines>(
            //    trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(new List<ICoverageLine> { coverageLine}))
            //    .Returns(trackedCoverageLines);
            //var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            //autoMoqer.Setup<ITrackedContainingCodeTrackerFactory, IContainingCodeTracker>(
            //    trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.Create(trackedCoverageLines))
            //    .Returns(containingCodeTracker);
            
            //var linesContainingCodeTrackerFactory = autoMoqer.Create<LinesContainingCodeTrackerFactory>();

            //Assert.That(linesContainingCodeTrackerFactory.Create(textSnapshot, mockLine.Object), Is.SameAs(containingCodeTracker));
        }

        [Test]
        public void Should_For_A_CodeRange_Should_Create_IContainingCodeTracker_From_TrackedCoverageLines_And_TrackingSpanRange()
        {
            throw new System.NotImplementedException();
            //var textSnapshot = new Mock<ITextSnapshot>().Object;
            //var mockLine = new Mock<ILine>();
            //mockLine.SetupGet(line => line.Number).Returns(5);
            //var mockLine2 = new Mock<ILine>();
            //mockLine2.SetupGet(line => line.Number).Returns(6);

            //var autoMoqer = new AutoMoqer();
            //var trackingSpan = new Mock<ITrackingSpan>().Object;
            //var trackingSpan2 = new Mock<ITrackingSpan>().Object;

            //autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, 4))
            //    .Returns(trackingSpan);
            //autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, 5))
            //    .Returns(trackingSpan2);

            //// for the CodeSpanRange no line adjustments - CodeSpanRange in reality will contain the lines
            //var codeRangeTrackingSpan20 = new Mock<ITrackingSpan>().Object;
            //var codeRangeTrackingSpan21 = new Mock<ITrackingSpan>().Object;
            //var codeRangeTrackingSpan22 = new Mock<ITrackingSpan>().Object;
            //autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, 20))
            //    .Returns(codeRangeTrackingSpan20);
            //autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, 21))
            //    .Returns(codeRangeTrackingSpan21);
            //autoMoqer.Setup<ITrackingLineFactory, ITrackingSpan>(trackingLineFactory => trackingLineFactory.Create(textSnapshot, 22))
            //    .Returns(codeRangeTrackingSpan22);
            //var trackingSpanRange = new Mock<ITrackingSpanRange>().Object;
            //autoMoqer.Setup<ITrackingSpanRangeFactory, ITrackingSpanRange>(
            //    trackingSpanRangeFactory => trackingSpanRangeFactory.Create(
            //        new List<ITrackingSpan> { codeRangeTrackingSpan20, codeRangeTrackingSpan21, codeRangeTrackingSpan22 }
            //    )).Returns(trackingSpanRange);


            //var coverageLine = new Mock<ICoverageLine>().Object;
            //var coverageLine2 = new Mock<ICoverageLine>().Object;
            //autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan, mockLine.Object))
            //     .Returns(coverageLine);
            //autoMoqer.Setup<ICoverageLineFactory, ICoverageLine>(coverageLineFactory => coverageLineFactory.Create(trackingSpan2, mockLine2.Object))
            //     .Returns(coverageLine2);
            //var trackedCoverageLines = new Mock<ITrackedCoverageLines>().Object;
            //autoMoqer.Setup<ITrackedCoverageLinesFactory, ITrackedCoverageLines>(
            //    trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(new List<ICoverageLine> { coverageLine, coverageLine2 }))
            //    .Returns(trackedCoverageLines);



            //var containingCodeTracker = new Mock<IContainingCodeTracker>().Object;
            //autoMoqer.Setup<ITrackedContainingCodeTrackerFactory, IContainingCodeTracker>(
            //    trackedContainingCodeTrackerFactory => trackedContainingCodeTrackerFactory.Create(trackingSpanRange,trackedCoverageLines))
            //    .Returns(containingCodeTracker);

            //var linesContainingCodeTrackerFactory = autoMoqer.Create<LinesContainingCodeTrackerFactory>();

            //Assert.That(linesContainingCodeTrackerFactory.Create(
            //    textSnapshot,new List<ILine> { mockLine.Object, mockLine2.Object},new CodeSpanRange(20,22)), Is.SameAs(containingCodeTracker));
        }
    }
}
