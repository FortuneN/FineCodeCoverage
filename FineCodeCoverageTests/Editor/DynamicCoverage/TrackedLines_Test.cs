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
        private IContainingCodeTrackerProcessResult GetProcessResult(List<Span> unprocessedSpans,bool changed)
        {
            var mockContainingCodeTrackerProcessResult = new Mock<IContainingCodeTrackerProcessResult>();
            mockContainingCodeTrackerProcessResult.SetupGet(containingCodeTrackerProcessResult => containingCodeTrackerProcessResult.UnprocessedSpans).Returns(unprocessedSpans);
            mockContainingCodeTrackerProcessResult.SetupGet(containingCodeTrackerProcessResult => containingCodeTrackerProcessResult.Changed).Returns(changed);
            return mockContainingCodeTrackerProcessResult.Object;
        }

        [TestCase(true,false,true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void Should_Process_Changes_With_Unprocessed_Spans(bool firstChanged, bool secondChanged,bool expectedChanged)
        {
            throw new System.NotImplementedException();
            //var textSnapshot = new Mock<ITextSnapshot>().Object;
            //var newSpanChanges = new List<Span> { new Span(10, 10) };
            //var unprocessedSpans = new List<Span> { new Span(15, 5) };
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(containingCodeTracker => containingCodeTracker.ProcessChanges(textSnapshot, newSpanChanges))
            //    .Returns(GetProcessResult(unprocessedSpans, firstChanged));
            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker2.Setup(containingCodeTracker => containingCodeTracker.ProcessChanges(textSnapshot, unprocessedSpans))
            //    .Returns(GetProcessResult(unprocessedSpans, secondChanged));
            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object, mockContainingCodeTracker2.Object });

            //Assert.That(trackedLines.Changed(textSnapshot, newSpanChanges),Is.EqualTo(expectedChanged));

            //mockContainingCodeTracker1.VerifyAll();
            //mockContainingCodeTracker2.VerifyAll();
           
        }

        [Test]
        public void Should_Skip_Further_ProcessChanges_When_No_Unprocesed_Spans()
        {
            //var textSnapshot = new Mock<ITextSnapshot>().Object;
            //var newSpanChanges = new List<Span> { new Span(10, 10) };
            //var unprocessedSpans = Enumerable.Empty<Span>().ToList();
            //var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            //mockContainingCodeTracker1.Setup(containingCodeTracker => containingCodeTracker.ProcessChanges(textSnapshot, newSpanChanges))
            //    .Returns(GetProcessResult(unprocessedSpans, true));
            //var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();

            //var trackedLines = new TrackedLines(new List<IContainingCodeTracker> { mockContainingCodeTracker1.Object, mockContainingCodeTracker2.Object });

            //trackedLines.Changed(textSnapshot, newSpanChanges);

            //mockContainingCodeTracker1.VerifyAll();
            //mockContainingCodeTracker2.VerifyNoOtherCalls();
            throw new System.NotImplementedException();
        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers()
        {
            //IDynamicLine CreateDynamicLine(int lineNumber)
            //{
            //    var mockDynamicLine = new Mock<IDynamicLine>();
            //    mockDynamicLine.SetupGet(x => x.Number).Returns(lineNumber);
            //    return mockDynamicLine.Object;
            //}

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
            //});

            //var lines = trackedLines.GetLines(10, 20);
            //Assert.That(lines,Is.EqualTo(expectedLines));
            throw new System.NotImplementedException();
        }

        [Test]
        public void Should_Return_Lines_From_ContainingCodeTrackers_Exiting_Early()
        {
            // IDynamicLine CreateDynamicLine(int lineNumber)
            // {
            //     var mockDynamicLine = new Mock<IDynamicLine>();
            //     mockDynamicLine.SetupGet(x => x.Number).Returns(lineNumber);
            //     return mockDynamicLine.Object;
            // }

            // var mockContainingCodeTracker1 = new Mock<IContainingCodeTracker>();
            // mockContainingCodeTracker1.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            // {
            //     CreateDynamicLine(10),
            // });
            // var mockContainingCodeTracker2 = new Mock<IContainingCodeTracker>();
            // mockContainingCodeTracker2.Setup(x => x.Lines).Returns(new List<IDynamicLine>
            // {
            //     CreateDynamicLine(21),
            // });

            // var notCalledMockContainingCodeTracker = new Mock<IContainingCodeTracker>(MockBehavior.Strict);

            // var trackedLines = new TrackedLines(new List<IContainingCodeTracker>
            // {
            //     mockContainingCodeTracker1.Object,
            //     mockContainingCodeTracker2.Object,
            //     notCalledMockContainingCodeTracker.Object
            // });

            //trackedLines.GetLines(10, 20).ToList();
            throw new System.NotImplementedException();
        }
    }
}
