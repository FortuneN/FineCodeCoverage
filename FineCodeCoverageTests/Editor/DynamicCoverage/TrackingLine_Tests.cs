using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackingLine_Tests
    {
        [TestCase(DynamicCoverageType.Dirty)]
        [TestCase(DynamicCoverageType.NotIncluded)]
        public void Should_Have_A_Line_From_The_Start_Point_When_Constructed(DynamicCoverageType coverageType)
        {
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, currentSnapshot, false)).Returns(10);

            var trackingLine = new TrackingLine(trackingSpan, currentSnapshot, mockLineTracker.Object, coverageType);

            AssertTrackingLine(trackingLine, 10, coverageType);
        }

        private void AssertTrackingLine(TrackingLine trackingLine, int lineNumber, DynamicCoverageType coverageType)
        {
            var dynamicLine = trackingLine.Line;

            Assert.That(coverageType, Is.EqualTo(dynamicLine.CoverageType));
            Assert.That(lineNumber, Is.EqualTo(dynamicLine.Number));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Have_An_Updated_Line_When_Update(bool changeLineNumber)
        {
            var initialSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, initialSnapshot, false)).Returns(10);

            var trackingLine = new TrackingLine(trackingSpan, initialSnapshot, mockLineTracker.Object, DynamicCoverageType.Dirty);

            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newLineNumber = changeLineNumber ? 11 : 10;
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, currentSnapshot, false))
                .Returns(newLineNumber);

            var updated = trackingLine.Update(currentSnapshot);
            Assert.That(updated, Is.EqualTo(changeLineNumber));
            AssertTrackingLine(trackingLine, newLineNumber, DynamicCoverageType.Dirty);
        }
    }
}
