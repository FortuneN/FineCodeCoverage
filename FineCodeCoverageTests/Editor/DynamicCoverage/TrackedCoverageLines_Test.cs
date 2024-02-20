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
        [TestCase(true, true, true)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        public void Should_Update_All_CoverageLine(bool firstUpdated, bool secondUpdated, bool expectedUpdated)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            Mock<ICoverageLine> CreateMockCoverageLine(bool coverageLineUpdated)
            {
                var mockCoverageLine = new Mock<ICoverageLine>();
                mockCoverageLine.Setup(coverageLine => coverageLine.Update(textSnapshot)).Returns(coverageLineUpdated);
                return mockCoverageLine;
            }

            var mockCoverageLines = new List<Mock<ICoverageLine>>
            {
                CreateMockCoverageLine(firstUpdated),
                CreateMockCoverageLine(secondUpdated)
            };

            var trackedCoverageLines = new TrackedCoverageLines(mockCoverageLines.Select(mock => mock.Object).ToList());
            

            var updated = trackedCoverageLines.Update(textSnapshot);

            mockCoverageLines.ForEach(mock => mock.VerifyAll());

            Assert.That(updated, Is.EqualTo(expectedUpdated));
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
