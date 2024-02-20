using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DirtyLine_Tests
    {
        [Test]
        public void Should_Have_An_Adjusted_Dirty_Line_From_The_Start_Point_When_Constructed()
        {
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetTrackedLineInfo(trackingSpan, currentSnapshot, false, false)).Returns(new TrackedLineInfo(10, ""));
            
            var dirtyLine = new DirtyLine(trackingSpan, currentSnapshot, mockLineTracker.Object);
            
            AssertDirtyLine(dirtyLine, 11);
        }

        private void AssertDirtyLine(DirtyLine dirtyLine, int expectedAdjustedLineNumber)
        {
            var dynamicLine = dirtyLine.Line;

            Assert.That(DynamicCoverageType.Dirty, Is.EqualTo(dynamicLine.CoverageType));
            Assert.That(expectedAdjustedLineNumber, Is.EqualTo(dynamicLine.Number));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Have_An_Updated_Dirty_Line_When_Update(bool changeLineNumber)
        {
            var initialSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetTrackedLineInfo(trackingSpan, initialSnapshot, false, false)).Returns(new TrackedLineInfo(10, ""));

            var dirtyLine = new DirtyLine(trackingSpan, initialSnapshot, mockLineTracker.Object);

            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newLineNumber = changeLineNumber ? 11 : 10;
            mockLineTracker.Setup(lineTracker => lineTracker.GetTrackedLineInfo(trackingSpan, currentSnapshot, false, false))
                .Returns(new TrackedLineInfo(newLineNumber, ""));

            var updated = dirtyLine.Update(currentSnapshot);
            Assert.That(updated, Is.EqualTo(changeLineNumber));
            AssertDirtyLine(dirtyLine, newLineNumber + 1);
        }
    }
}
