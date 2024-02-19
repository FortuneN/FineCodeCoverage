using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DirtyLine_Tests
    {
        private DirtyLine dirtyLine;
        private Mock<ITrackingSpan> mockTrackingSpan;
        private void SetUpMocks(Mock<ITextSnapshot> mockCurrentSnapshot, int lineNumber)
        {
            mockCurrentSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(100);
            
            var snapshotPoint = new SnapshotPoint(mockCurrentSnapshot.Object, lineNumber);
            mockTrackingSpan.Setup(startTrackingSpan => startTrackingSpan.GetStartPoint(mockCurrentSnapshot.Object)).Returns(snapshotPoint);
            mockCurrentSnapshot.Setup(currentSnapshot => currentSnapshot.GetLineNumberFromPosition(snapshotPoint)).Returns(lineNumber);
        }

        [SetUp]
        public void Setup()
        {
            var mockCurrentSnapshot = new Mock<ITextSnapshot>();
            mockTrackingSpan = new Mock<ITrackingSpan>();

            SetUpMocks(mockCurrentSnapshot,10);
            dirtyLine = new DirtyLine(mockTrackingSpan.Object, mockCurrentSnapshot.Object);
        }
        
        [Test]
        public void Should_Have_An_Adjusted_Dirty_Line_From_The_Start_Point_When_Constructed_()
        {
            AssertDirtyLine(11);
        }

        private void AssertDirtyLine(int expectedAdjustedLineNumber)
        {
            var dynamicLine = dirtyLine.Line;

            Assert.That(DynamicCoverageType.Dirty, Is.EqualTo(dynamicLine.CoverageType));
            Assert.That(expectedAdjustedLineNumber, Is.EqualTo(dynamicLine.Number));
        }

        [Test]
        public void Should_Have_An_Updated_Dirty_Line_When_Update()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            SetUpMocks(mockTextSnapshot, 5);

            dirtyLine.Update(mockTextSnapshot.Object);
            AssertDirtyLine(6);
        }
    }
}
