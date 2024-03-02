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
            }, null);

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
            },null);

            
            var changed = newCodeTracker.ProcessChanges(currentTextSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 3, 3),
            },null);

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
            }, null);
            newCodeTracker.ProcessChanges(currentTextSnapshot, new List<SpanAndLineRange> {
                new SpanAndLineRange(new Span(0, 0), 2, 2),
            },null);

            Assert.That(newCodeTracker.Lines, Is.EqualTo(new List<IDynamicLine> { secondDynamicLine, firstDynamicLine }));
            
        }

        [Test]
        public void Should_Track_Line_Numbers_Passed_To_Ctor()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var mockTrackedNewCodeLine1 = new Mock<ITrackedNewCodeLine>();
            mockTrackedNewCodeLine1.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("exclude");
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory => 
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine1.Object);
            var mockTrackedNewCodeLine2 = new Mock<ITrackedNewCodeLine>();
            mockTrackedNewCodeLine2.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("notexclude");
            var expectedLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine2.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(expectedLine);
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 2)
            ).Returns(mockTrackedNewCodeLine2.Object);
            var mockLineExcluder = new Mock<ILineExcluder>();
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("exclude", true)).Returns(true);
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("notexclude", true)).Returns(false);

            var newCodeTracker = new NewCodeTracker(true, mockTrackedNewCodeLineFactory.Object, mockLineExcluder.Object, new List<int> { 1, 2 }, textSnapshot);

            var line = newCodeTracker.Lines.Single();
            Assert.That(line, Is.SameAs(expectedLine));
        }

        [Test]
        public void Should_Use_New_Code_CodeSpanRanges_StartLine_For_TrackedNewCodeLines()
        {
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var mockTrackedNewCodeLine1 = new Mock<ITrackedNewCodeLine>();
            var dynamicLine1 = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine1.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine1);
            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(currentSnapshot, SpanTrackingMode.EdgeExclusive, 5)
            ).Returns(mockTrackedNewCodeLine1.Object);
            var mockTrackedNewCodeLine2 = new Mock<ITrackedNewCodeLine>();
            var dynamicLine2 = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine2.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine2);
            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(currentSnapshot, SpanTrackingMode.EdgeExclusive, 15)
            ).Returns(mockTrackedNewCodeLine2.Object);

            var newCodeTracker = new NewCodeTracker(true, mockTrackedNewCodeLineFactory.Object, new Mock<ILineExcluder>().Object,new List<int> { },null);
            
            var newCodeCodeSpanRanges = new List<CodeSpanRange>
            {
                new CodeSpanRange(5,10),
                new CodeSpanRange(15,20)
            };

            var changed = newCodeTracker.ProcessChanges(currentSnapshot, null, newCodeCodeSpanRanges);
            
            Assert.That(changed, Is.True);
            var lines = newCodeTracker.Lines.ToList();
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0], Is.SameAs(dynamicLine1));
            Assert.That(lines[1], Is.SameAs(dynamicLine2));
           
        }

        [Test]
        public void Should_Remove_TrackedNewCodeLines_If_Not_In_NewCodeCodeSpanRanges()
        {
            (ITrackedNewCodeLine, IDynamicLine) SetupTrackedNewCodeLine(int lineNumber)
            {
                var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
                var mockDynamicLine = new Mock<IDynamicLine>();
                mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(lineNumber);
                mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(mockDynamicLine.Object);
                return (mockTrackedNewCodeLine.Object, mockDynamicLine.Object);
            }
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var (trackedNewCodeLine1, dynamicLine1) = SetupTrackedNewCodeLine(5);
            var (trackedNewCodeLine2, dynamicLine2) = SetupTrackedNewCodeLine(6);

            mockTrackedNewCodeLineFactory.SetupSequence(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(It.IsAny<ITextSnapshot>(), SpanTrackingMode.EdgeExclusive, It.IsAny<int>())
            ).Returns(trackedNewCodeLine1).Returns(trackedNewCodeLine2);
            


            var newCodeTracker = new NewCodeTracker(
                true, 
                mockTrackedNewCodeLineFactory.Object, 
                new Mock<ILineExcluder>().Object, 
                new List<int> { 5, 6}, 
                new Mock<ITextSnapshot>().Object
            );
            AssertLines(dynamicLine1, dynamicLine2);
            

            var newCodeCodeSpanRanges = new List<CodeSpanRange>
            {
                new CodeSpanRange(5,10)
            };

            var changed = newCodeTracker.ProcessChanges(new Mock<ITextSnapshot>().Object, null, newCodeCodeSpanRanges);

            Assert.That(changed, Is.True);
            AssertLines(dynamicLine1);

            void AssertLines(params IDynamicLine[] dynamicLines)
            {
                var lines = newCodeTracker.Lines.ToList();
                Assert.That(lines.Count, Is.EqualTo(dynamicLines.Length));
               for(var i = 0; i < dynamicLines.Length; i++)
                {
                    Assert.That(lines[i], Is.SameAs(dynamicLines[i]));
                }
            }
        }

        [Test]
        public void Should_Be_Unchanged_If_Existing_Lines_Are_New_Code_CodeSpanRange_Start_Lines()
        {
            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(5);
            mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(mockDynamicLine.Object);

            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();

            mockTrackedNewCodeLineFactory.Setup(
                trackedNewCodeLineFactory => trackedNewCodeLineFactory.Create(It.IsAny<ITextSnapshot>(), SpanTrackingMode.EdgeExclusive, It.IsAny<int>())
            ).Returns(mockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                true,
                mockTrackedNewCodeLineFactory.Object,
                new Mock<ILineExcluder>().Object,
                new List<int> { 5},
                new Mock<ITextSnapshot>().Object
            );


            var newCodeCodeSpanRanges = new List<CodeSpanRange>
            {
                new CodeSpanRange(5,10)
            };

            var changed = newCodeTracker.ProcessChanges(new Mock<ITextSnapshot>().Object, null, newCodeCodeSpanRanges);

            Assert.That(changed, Is.False);
            
        }
    }
}
