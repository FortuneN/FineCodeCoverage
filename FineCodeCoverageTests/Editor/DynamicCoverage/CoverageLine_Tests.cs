using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CoverageLine_Tests
    {
        [Test]
        public void Should_Have_A_Dirty_IDynamicLine_When_Dirty()
        {
            //var autoMoqer = new AutoMoqer();
            //var coverageLine = autoMoqer.Create<CoverageLine>();
            //coverageLine.Dirty();
            //Assert.IsTrue(coverageLine.Line.IsDirty);
            throw new System.NotImplementedException();
        }

        [Test]
        public void Should_Return_Update_Type_Removal_When_Snapshot_Span_Is_Empty()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLine = autoMoqer.Create<CoverageLine>();
            var currentSnapshot = autoMoqer.GetMock<ITextSnapshot>();
            var trackingSpan = autoMoqer.GetMock<ITrackingSpan>();
            trackingSpan.Setup(t => t.GetSpan(currentSnapshot.Object)).Returns(new SnapshotSpan());
            var result = coverageLine.Update(currentSnapshot.Object);
            Assert.AreEqual(CoverageLineUpdateType.Removal, result);
        }

        private (CoverageLineUpdateType,CoverageLine) TestLineNumberUpdate(int initialLineNumber, int newLineNumber)
        {
            var autoMoqer = new AutoMoqer();
            var mockCurrentSnapshot = autoMoqer.GetMock<ITextSnapshot>();
            mockCurrentSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(100);
            mockCurrentSnapshot.Setup(currentSnapshot => currentSnapshot.GetLineNumberFromPosition(10)).Returns(newLineNumber);
            var mockTrackingSpan = autoMoqer.GetMock<ITrackingSpan>();
            mockTrackingSpan.Setup(t => t.GetSpan(mockCurrentSnapshot.Object))
                .Returns(new SnapshotSpan(new SnapshotPoint(mockCurrentSnapshot.Object, 10), 20));
            var mockLine = autoMoqer.GetMock<ILine>();
            mockLine.SetupGet(l => l.Number).Returns(initialLineNumber);


            var coverageLine = autoMoqer.Create<CoverageLine>();

            var result = coverageLine.Update(mockCurrentSnapshot.Object);

            return (result, coverageLine);
        }

        [Test]
        public void Should_Return_Update_Type_LineNumberChange_And_Change_Line_Number_When_LineNumber_Changes()
        {
            var newLineNumber = 1;
            var (result, coverageLine) = TestLineNumberUpdate(1, newLineNumber);
            
            Assert.AreEqual(CoverageLineUpdateType.LineNumberChange, result);
            var adjustedLineNumber = newLineNumber + 1;
            Assert.AreEqual(adjustedLineNumber, coverageLine.Line.Number);
        }

        [Test]
        public void Should_Return_Update_Type_NoChange_When_LineNumber_Does_Not_Change()
        {
            var (result, coverageLine) = TestLineNumberUpdate(1,0);

            Assert.AreEqual(CoverageLineUpdateType.NoChange, result);

            Assert.That(coverageLine.Line.Number, Is.EqualTo(1));
        }

        [TestCase(CoverageType.Covered)]
        [TestCase(CoverageType.NotCovered)]
        [TestCase(CoverageType.Partial)]
        public void Should_Have_Line_With_Correct_CoverageType(CoverageType coverageType)
        {
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
            Assert.That(new CoverageLine(null, mockLine.Object).Line.CoverageType, Is.EqualTo(coverageType));
        }

    }
}
