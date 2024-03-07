using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackedCoverageLines_Tests
    {
        [Test]
        public void Should_Update_All_CoverageLine()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            Mock<ICoverageLine> CreateMockCoverageLine(List<int> updatedCoverageLines)
            {
                var mockCoverageLine = new Mock<ICoverageLine>();
                mockCoverageLine.Setup(coverageLine => coverageLine.Update(textSnapshot)).Returns(updatedCoverageLines);
                return mockCoverageLine;
            }

            var mockCoverageLines = new List<Mock<ICoverageLine>>
            {
                CreateMockCoverageLine(new List<int>{ 1,2}),
                CreateMockCoverageLine(new List<int>{3,4})
            };

            var trackedCoverageLines = new TrackedCoverageLines(mockCoverageLines.Select(mock => mock.Object).ToList());


            var updatedLineNumbers = trackedCoverageLines.GetUpdatedLineNumbers(textSnapshot).ToList();

            mockCoverageLines.ForEach(mock => mock.Verify());

            Assert.That(updatedLineNumbers, Is.EqualTo(new List<int> { 1,2,3,4}));
        }

        [Test]
        public void Should_Return_Lines_From_CoverageLines()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            (ICoverageLine, IDynamicLine) SetUpCoverageLine()
            {
                var mockCoverageLine = new Mock<ICoverageLine>();
                var dynamicLine = new Mock<IDynamicLine>().Object;
                mockCoverageLine.SetupGet(coverageLine => coverageLine.Line).Returns(dynamicLine);
                return (mockCoverageLine.Object, dynamicLine);
            }

            var (firstCoverageLine, firstDynamicLine) = SetUpCoverageLine();
            var (secondCoverageLine, secondDynamicLine) = SetUpCoverageLine();
            var trackedCoverageLines = new TrackedCoverageLines(new List<ICoverageLine> { firstCoverageLine, secondCoverageLine});

            var lines = trackedCoverageLines.Lines.ToList();

            Assert.That(lines.Count(), Is.EqualTo(2));
            Assert.That(lines[0], Is.SameAs(firstDynamicLine));
            Assert.That(lines[1], Is.SameAs(secondDynamicLine));


        }
    }
}
