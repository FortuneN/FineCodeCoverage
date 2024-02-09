using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Coverage_Colours
{
    internal class TrackedLines_Test
    {
        [TestCase(true,true,true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void Should_IContainingCodeTracker_ProcessChanges_For_All_When_Changed(bool firstChanged, bool secondChanged,bool expectedChanged)
        {
            var textSnapShot = new Mock<ITextSnapshot>().Object;
            var newSpanChanges = new List<Span>
            {
                new Span(10,20)
            };

            var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker1.Setup(x => x.ProcessChanges(textSnapShot, newSpanChanges)).Returns(firstChanged);
            var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker2.Setup(x => x.ProcessChanges(textSnapShot, newSpanChanges)).Returns(secondChanged);
            var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            {
                mockContainingCodeTracker1.Object,
                mockContainingCodeTracker2.Object
            });

            var changed = trackedLines.Changed(textSnapShot, newSpanChanges);

            Assert.That(changed, Is.EqualTo(expectedChanged));

        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers()
        {
            IDynamicLine CreateDynamicLine(int lineNumber)
            {
                var mockDynamicLine = new Mock<IDynamicLine>();
                mockDynamicLine.SetupGet(x => x.Number).Returns(lineNumber);
                return mockDynamicLine.Object;
            }

            var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            var expectedLines = new List<IDynamicLine>
            {
                CreateDynamicLine(10),
                CreateDynamicLine(11),
                CreateDynamicLine(18),
                CreateDynamicLine(19),
                CreateDynamicLine(20),
            };
            mockContainingCodeTracker1.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            {
                CreateDynamicLine(9),
                expectedLines[0],
                expectedLines[1]
            });
            var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker2.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            {
                expectedLines[2],
                expectedLines[3],
                expectedLines[4],
            });

            var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            {
                mockContainingCodeTracker1.Object,
                mockContainingCodeTracker2.Object
            });

            var lines = trackedLines.GetLines(10, 20);
            Assert.That(lines,Is.EqualTo(expectedLines));
        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers_Exiting_Early()
        {
            IDynamicLine CreateDynamicLine(int lineNumber)
            {
                var mockDynamicLine = new Mock<IDynamicLine>();
                mockDynamicLine.SetupGet(x => x.Number).Returns(lineNumber);
                return mockDynamicLine.Object;
            }

            var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker1.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            {
                CreateDynamicLine(10),
            });
            var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            mockContainingCodeTracker2.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            {
                CreateDynamicLine(21),
            });

            var notCalledMockContainingCodeTracker = new Mock<IContainingCodeTracker>(MockBehavior.Strict);

            var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            {
                mockContainingCodeTracker1.Object,
                mockContainingCodeTracker2.Object,
                notCalledMockContainingCodeTracker.Object
            });

           trackedLines.GetLines(10, 20).ToList();
        }
    }
}
