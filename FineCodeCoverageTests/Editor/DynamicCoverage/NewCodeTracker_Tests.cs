using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class NewCodeTracker_Tests
    {
        [Test]
        public void Should_Have_No_Lines_Initially()
        {
            var newCodeTracker = new NewCodeTracker(true, null,null);

            Assert.That(newCodeTracker.Lines, Is.Empty);
        }

        
        [TestCase(false, true)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void Should_Have_A_New_Line_For_All_New_Code_Based_Upon_Language(bool isCSharp,bool exclude)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var newDynamicCodeLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(newDynamicCodeLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("text");
            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(textSnapshot,SpanTrackingMode.EdgeExclusive, 2)
            ).Returns(mockTrackedNewCodeLine.Object);

            var mockCodeLineExcluder = new Mock<ILineExcluder>();
            mockCodeLineExcluder.Setup(codeLineExcluder => codeLineExcluder.ExcludeIfNotCode("text", isCSharp)).Returns(exclude);
            var newCodeTracker = new NewCodeTracker(isCSharp, mockTrackedNewCodeLineFactory.Object,mockCodeLineExcluder.Object);

            var changed = newCodeTracker.ProcessChanges(textSnapshot, new List<SpanAndLineRange> { 
                new SpanAndLineRange(new Span(0, 0), 2, 2),
                new SpanAndLineRange(new Span(3, 3), 2, 2),
            });

            Assert.That(changed, Is.EqualTo(!exclude));
            Assert.That(newCodeTracker.Lines.Count, Is.EqualTo(exclude ? 0 : 1));
            if (!exclude)
            {
                Assert.That(newCodeTracker.Lines.First(), Is.SameAs(newDynamicCodeLine));
            }
        }

        // order

        [TestCase(true, true, false)]
        [TestCase(true, false,true)]
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void Should_Update_And_Possibly_Remove_Existing_Lines(bool isCSharp,bool lineUpdated,bool exclude)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var newDynamicCodeLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(newDynamicCodeLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("text");
            
            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 2)
            ).Returns(mockTrackedNewCodeLine.Object);

            var mockCodeLineExcluder = new Mock<ILineExcluder>();
            mockCodeLineExcluder.Setup(codeLineExcluder => codeLineExcluder.ExcludeIfNotCode("text", isCSharp)).Returns(false);

            // second invocation setup
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("updated text", 3, lineUpdated));
            mockCodeLineExcluder.Setup(codeLineExcluder => codeLineExcluder.ExcludeIfNotCode("updated text", isCSharp)).Returns(exclude);

            var newCodeTracker = new NewCodeTracker(isCSharp, mockTrackedNewCodeLineFactory.Object, mockCodeLineExcluder.Object);

            newCodeTracker.ProcessChanges(textSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 2, 2),
            });

            
            var changed = newCodeTracker.ProcessChanges(currentTextSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 3, 3),
            });

            var expectedChanged = exclude || changed;
            Assert.That(changed, Is.EqualTo(expectedChanged));
            Assert.That(newCodeTracker.Lines.Count(), Is.EqualTo(exclude ? 0 : 1));
        }

        [Test]
        public void Should_Return_Lines_Ordered_By_Line_Number()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            IDynamicLine MockDynamicLine(Mock<ITrackedNewCodeLine> mockTrackedNewCodeLine, int lineNumber)
            {
                var mockNewDynamicCodeLine = new Mock<IDynamicLine>();
                mockNewDynamicCodeLine.SetupGet(l => l.Number).Returns(lineNumber);
                mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(mockNewDynamicCodeLine.Object);
                return mockNewDynamicCodeLine.Object;
            }

            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var mockFirstTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var firstDynamicLine = MockDynamicLine(mockFirstTrackedNewCodeLine, 4);
            mockFirstTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("text");
            mockFirstTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("text", 4, false));

            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 4)
            ).Returns(mockFirstTrackedNewCodeLine.Object);
            var mockSecondTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var secondDynamicLine = MockDynamicLine(mockSecondTrackedNewCodeLine, 2);
            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(currentTextSnapshot, SpanTrackingMode.EdgeExclusive, 2)
            ).Returns(mockSecondTrackedNewCodeLine.Object);

            var mockCodeLineExcluder = new Mock<ILineExcluder>();
            mockCodeLineExcluder.Setup(codeLineExcluder => codeLineExcluder.ExcludeIfNotCode(It.IsAny<string>(), true)).Returns(false);

            var newCodeTracker = new NewCodeTracker(true, mockTrackedNewCodeLineFactory.Object, mockCodeLineExcluder.Object);

            newCodeTracker.ProcessChanges(textSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 4, 4),
            });
            newCodeTracker.ProcessChanges(currentTextSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 2, 2),
            });

            Assert.That(newCodeTracker.Lines, Is.EqualTo(new List<IDynamicLine> { secondDynamicLine, firstDynamicLine }));
            
        }
    }
}
