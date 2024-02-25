using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CoverageLine_Tests
    {
        [TestCase(CoverageType.Covered, DynamicCoverageType.Covered)]
        [TestCase(CoverageType.NotCovered, DynamicCoverageType.NotCovered)]
        [TestCase(CoverageType.Partial, DynamicCoverageType.Partial)]
        public void Should_Have_A_DynamicLine_From_ILine_When_Constructed(CoverageType lineCoverageType, DynamicCoverageType expectedDynamicCoverageType)
        {
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(l => l.CoverageType).Returns(lineCoverageType);
            mockLine.SetupGet(l => l.Number).Returns(1);

            var coverageLine = new CoverageLine(null, mockLine.Object, null);

            Assert.That(coverageLine.Line.CoverageType, Is.EqualTo(expectedDynamicCoverageType));
            Assert.That(coverageLine.Line.Number, Is.EqualTo(0));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Updated_If_The_Line_Number_Changes(bool updateLineNumber)
        {
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(l => l.Number).Returns(1);

            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;
            var mockLineTracker = new Mock<ILineTracker>();

            var updatedLineNumber = updateLineNumber ? 10 : 0;
            mockLineTracker.Setup(lineTracker => lineTracker.GetLineNumber(trackingSpan, currentTextSnapshot, true))
                .Returns(updatedLineNumber);
            var coverageLine = new CoverageLine(trackingSpan, mockLine.Object, mockLineTracker.Object);

            var updated = coverageLine.Update(currentTextSnapshot);

            Assert.That(updated, Is.EqualTo(updateLineNumber));

            Assert.That(coverageLine.Line.Number, Is.EqualTo(updatedLineNumber));
        }
    }
}
