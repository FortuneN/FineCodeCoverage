using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackedLines_Test
    {
        private IContainingCodeTrackerProcessResult GetProcessResult(List<SpanAndLineRange> unprocessedSpans,bool changed = true,bool isEmpty = false)
        {
            var mockContainingCodeTrackerProcessResult = new Mock<IContainingCodeTrackerProcessResult>();
            mockContainingCodeTrackerProcessResult.SetupGet(containingCodeTrackerProcessResult => containingCodeTrackerProcessResult.UnprocessedSpans).Returns(unprocessedSpans);
            mockContainingCodeTrackerProcessResult.SetupGet(containingCodeTrackerProcessResult => containingCodeTrackerProcessResult.Changed).Returns(changed);
            mockContainingCodeTrackerProcessResult.SetupGet(containingCodeTrackerProcessResult => containingCodeTrackerProcessResult.IsEmpty).Returns(isEmpty);
            return mockContainingCodeTrackerProcessResult.Object;
        }

        [Test]
        public void Should_Process_Changes_With_Unprocessed_Spans()
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();
            //var newSpanChanges = new List<Span> { new Span(10, 10) };
            //mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(10)).Returns(1);
            //mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(20)).Returns(2);

            //var changes = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(15, 5), 1, 1) };
            //var unprocessedSpans = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(10, 5), 0, 1) };
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(
            //    containingCodeTracker => containingCodeTracker.ProcessChanges(
            //        mockTextSnapshot.Object, 
            //        new List<SpanAndLineRange> { new SpanAndLineRange(newSpanChanges[0],1,2) }))
            //    .Returns(GetProcessResult(unprocessedSpans));

            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker2.Setup(containingCodeTracker => containingCodeTracker.ProcessChanges(mockTextSnapshot.Object, unprocessedSpans))
            //    .Returns(GetProcessResult(unprocessedSpans));

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object, mockContainingCodeTracker2.Object }, null);
            //trackedLines.Changed(mockTextSnapshot.Object, newSpanChanges);

            //mockContainingCodeTracker1.VerifyAll();
            //mockContainingCodeTracker2.VerifyAll();

        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Changed_If_ContainingCodeTracker_Changed(bool firstChanged)
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();

            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(
            //    containingCodeTracker => containingCodeTracker.ProcessChanges(
            //        mockTextSnapshot.Object,
            //        It.IsAny<List<SpanAndLineRange>>()))
            //    .Returns(GetProcessResult(new List<SpanAndLineRange>(),firstChanged));

            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker2.Setup(containingCodeTracker => containingCodeTracker.ProcessChanges(mockTextSnapshot.Object, It.IsAny<List<SpanAndLineRange>>()))
            //    .Returns(GetProcessResult(new List<SpanAndLineRange>(), !firstChanged));

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object, mockContainingCodeTracker2.Object }, null);
            //var changed = trackedLines.Changed(mockTextSnapshot.Object, new List<Span>());

            //Assert.That(changed, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Remove_ContainingCodeTracker_When_Empty(bool isEmpty)
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();

            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(
            //    containingCodeTracker => containingCodeTracker.ProcessChanges(
            //        mockTextSnapshot.Object,
            //        It.IsAny<List<SpanAndLineRange>>()))
            //    .Returns(GetProcessResult(new List<SpanAndLineRange>(), false, isEmpty));

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object}, null);
            //Assert.That(trackedLines.ContainingCodeTrackers, Is.EquivalentTo(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object }));
            //trackedLines.Changed(mockTextSnapshot.Object, new List<Span>());
            //trackedLines.Changed(mockTextSnapshot.Object, new List<Span>());

            //var times = isEmpty ? Times.Once() : Times.Exactly(2);
            //mockContainingCodeTracker1.Verify(
            //    containingCodeTracker => containingCodeTracker.ProcessChanges(mockTextSnapshot.Object, It.IsAny<List<SpanAndLineRange>>()), times);
            //Assert.That(trackedLines.ContainingCodeTrackers, Has.Count.EqualTo(isEmpty ? 0 : 1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Process_NewCodeTracker_Changes_After_ContainingCodeTrackers(bool newCodeChanged)
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();

            //var unprocessedSpans = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 10), 0, 1) };
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(
            //    containingCodeTracker => containingCodeTracker.ProcessChanges(
            //        mockTextSnapshot.Object,
            //        It.IsAny<List<SpanAndLineRange>>()))
            //    .Returns(GetProcessResult(unprocessedSpans, false));

            //var mockNewCodeTracker = new Mock<INewCodeTracker>();
            //mockNewCodeTracker.Setup(newCodeTracker => newCodeTracker.ProcessChanges(mockTextSnapshot.Object, unprocessedSpans))
            //    .Returns(newCodeChanged);

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object }, mockNewCodeTracker.Object);
            //var changed = trackedLines.Changed(mockTextSnapshot.Object, new List<Span>());

            //Assert.That(changed, Is.EqualTo(newCodeChanged));

        }

        private static IDynamicLine CreateDynamicLine(int lineNumber)
        {
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(x => x.Number).Returns(lineNumber);
            return mockDynamicLine.Object;
        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers()
        {
            throw new System.NotImplementedException();
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //var expectedLines = new List<IDynamicLine>
            //{
            //    CreateDynamicLine(10),
            //    CreateDynamicLine(11),
            //    CreateDynamicLine(18),
            //    CreateDynamicLine(19),
            //    CreateDynamicLine(20),
            //};
            //mockContainingCodeTracker1.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            //{
            //    CreateDynamicLine(9),
            //    expectedLines[0],
            //    expectedLines[1]
            //});
            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker2.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            //{
            //    expectedLines[2],
            //    expectedLines[3],
            //    expectedLines[4],
            //});

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            //{
            //    mockContainingCodeTracker1.Object,
            //    mockContainingCodeTracker2.Object
            //},null);

            //var lines = trackedLines.GetLines(10, 20);
            //Assert.That(lines, Is.EqualTo(expectedLines));
        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers_Exiting_Early()
        {
            throw new System.NotImplementedException();
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            // {
            //     CreateDynamicLine(10),
            // });
            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker2.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            // {
            //     CreateDynamicLine(21),
            // });

            //var notCalledMockContainingCodeTracker = new Mock<IContainingCodeTracker>(MockBehavior.Strict);

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            // {
            //     mockContainingCodeTracker1.Object,
            //     mockContainingCodeTracker2.Object,
            //     notCalledMockContainingCodeTracker.Object
            // },null);

            //var lines = trackedLines.GetLines(10, 20).ToList();

            //mockContainingCodeTracker1.VerifyAll();
            //mockContainingCodeTracker2.VerifyAll();
        }

        [Test]
        public void Should_Return_Lines_From_NewCodeTracker_But_Not_If_Already_From_ContainingCodeTrackers()
        {
            throw new System.NotImplementedException();
            //var expectedLines = new List<IDynamicLine>
            //{
            //    CreateDynamicLine(10),
            //    CreateDynamicLine(15),
            //};
            //var mockContainingCodeTracker = new Mock<IContainingCodeTracker>();

            //mockContainingCodeTracker.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            // {
            //     expectedLines[0]
            // });

            //var mockNewCodeTracker = new Mock<INewCodeTracker>();
            //mockNewCodeTracker.SetupGet(newCodeTracker => newCodeTracker.Lines).Returns(new List<IDynamicLine>
            //{
            //     CreateDynamicLine(2),
            //     CreateDynamicLine(10),
            //     expectedLines[1],
            //     CreateDynamicLine(50),
            // });
            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            // {
            //     mockContainingCodeTracker.Object,
            // }, mockNewCodeTracker.Object);

            //var lines = trackedLines.GetLines(10, 20).ToList();
            //Assert.That(lines, Is.EqualTo(expectedLines));
        }
    }
}
