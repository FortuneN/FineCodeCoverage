using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackedCoverageLines_Tests
    {
        [TestCase(CoverageLineUpdateType.Removal,true)]
        [TestCase(CoverageLineUpdateType.LineNumberChange, true)]
        [TestCase(CoverageLineUpdateType.NoChange, false)]
        public void Should_Be_Changed_When_A_Coverage_Line_Has_Changed(CoverageLineUpdateType coverageLineUpdateType, bool expectedChange)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockCoverageLine = new Mock<ICoverageLine>();
            mockCoverageLine.Setup(coverageLine => coverageLine.Update(textSnapshot)).Returns(coverageLineUpdateType);
            var mockCoverageLine2 = new Mock<ICoverageLine>();
            mockCoverageLine2.Setup(coverageLine => coverageLine.Update(textSnapshot)).Returns(CoverageLineUpdateType.NoChange);

            var trackedCoverageLines = new TrackedCoverageLines(new List<ICoverageLine> { mockCoverageLine.Object, mockCoverageLine2.Object });

            var changed = trackedCoverageLines.Update(textSnapshot);
            Assert.AreEqual(expectedChange, changed);
        }

        private static IDynamicLine CreateDynamicLIne(int lineNumber)
        {
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(dl => dl.Number).Returns(lineNumber);
            return mockDynamicLine.Object;
        }

        [Test]
        public void Should_Return_Lines_From_All_CoverageLine()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var dynamicLine = CreateDynamicLIne(1);
            var dynamicLine2 = CreateDynamicLIne(2);
            var mockCoverageLine = new Mock<ICoverageLine>();
            mockCoverageLine.SetupGet(coverageLine => coverageLine.Line).Returns(dynamicLine);
            var mockCoverageLine2 = new Mock<ICoverageLine>();
            mockCoverageLine2.SetupGet(coverageLine => coverageLine.Line).Returns(dynamicLine2);

            var trackedCoverageLines = new TrackedCoverageLines(new List<ICoverageLine> { mockCoverageLine.Object, mockCoverageLine2.Object });

            Assert.That(trackedCoverageLines.Lines, Is.EqualTo(new List<IDynamicLine> { dynamicLine, dynamicLine2 }));
        }

        [Test]
        public void Should_Remove_CoverageLine_When_It_Is_Removed()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var dynamicLine = CreateDynamicLIne(1);
            var dynamicLine2 = CreateDynamicLIne(2);
            var mockCoverageLine = new Mock<ICoverageLine>();
            mockCoverageLine.SetupGet(coverageLine => coverageLine.Line).Returns(dynamicLine);
            var mockCoverageLine2 = new Mock<ICoverageLine>();
            mockCoverageLine2.SetupGet(coverageLine => coverageLine.Line).Returns(dynamicLine2);
            mockCoverageLine2.Setup(coverageLine => coverageLine.Update(textSnapshot)).Returns(CoverageLineUpdateType.Removal);

            var trackedCoverageLines = new TrackedCoverageLines(new List<ICoverageLine> { mockCoverageLine.Object, mockCoverageLine2.Object });

            trackedCoverageLines.Update(textSnapshot);

            Assert.That(trackedCoverageLines.Lines, Is.EqualTo(new List<IDynamicLine> { dynamicLine }));


        }
    }
}
