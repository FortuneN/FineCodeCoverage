using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DirtyLine_Tests
    {
        [Test]
        public void Should_Have_A_Dirty_Line_From_The_Start_Point_When_Constructed()
        {
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, currentSnapshot, false)).Returns(10);
            
            var dirtyLine = new DirtyLine(trackingSpan, currentSnapshot, mockLineTracker.Object);
            
            AssertDirtyLine(dirtyLine, 10);
        }

        private void AssertDirtyLine(DirtyLine dirtyLine, int lineNumber)
        {
            var dynamicLine = dirtyLine.Line;

            Assert.That(DynamicCoverageType.Dirty, Is.EqualTo(dynamicLine.CoverageType));
            Assert.That(lineNumber, Is.EqualTo(dynamicLine.Number));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Have_An_Updated_Dirty_Line_When_Update(bool changeLineNumber)
        {
            var initialSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;

            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, initialSnapshot, false)).Returns(10);

            var dirtyLine = new DirtyLine(trackingSpan, initialSnapshot, mockLineTracker.Object);

            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newLineNumber = changeLineNumber ? 11 : 10;
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, currentSnapshot, false))
                .Returns(newLineNumber);

            var updated = dirtyLine.Update(currentSnapshot);
            Assert.That(updated, Is.EqualTo(changeLineNumber));
            AssertDirtyLine(dirtyLine, newLineNumber);
        }
    }
}
