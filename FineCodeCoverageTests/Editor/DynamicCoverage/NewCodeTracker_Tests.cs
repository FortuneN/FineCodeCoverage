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
        public void Should_Have_No_Lines_Initially_When_Passed_No_Line_Numbers()
        {
            var newCodeTracker = new NewCodeTracker(null,null);

            Assert.That(newCodeTracker.Lines, Is.Empty);
        }


        [Test]
        public void Should_Track_Not_Excluded_Line_Numbers_Passed_To_Ctor()
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
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("exclude")).Returns(true);
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("notexclude")).Returns(false);

            var newCodeTracker = new NewCodeTracker(mockTrackedNewCodeLineFactory.Object, mockLineExcluder.Object, new List<int> { 1, 2 }, textSnapshot);

            var line = newCodeTracker.Lines.Single();
            Assert.That(line, Is.SameAs(expectedLine));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_Lines_Ordered_By_Line_Number(bool reverseOrder)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            var mockFirstDynamicLine = new Mock<IDynamicLine>();
            mockFirstDynamicLine.SetupGet(l => l.Number).Returns(reverseOrder ? 2 : 1);
            var firstDynamicLine = mockFirstDynamicLine.Object;
            var mockFirstTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            mockFirstTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(firstDynamicLine);
            var mockSecondTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var mockSecondDynamicLine = new Mock<IDynamicLine>();
            mockSecondDynamicLine.SetupGet(l => l.Number).Returns(reverseOrder ? 1 : 2);
            var secondDynamicLine = mockSecondDynamicLine.Object;
            mockSecondTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(secondDynamicLine);
            mockTrackedNewCodeLineFactory.SetupSequence(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, It.IsAny<int>())
            ).Returns(mockFirstTrackedNewCodeLine.Object)
            .Returns(mockSecondTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object,
                new Mock<ILineExcluder>().Object,
                new List<int> { 1, 2 },
                textSnapshot
            );

            Assert.That(
                newCodeTracker.Lines,
                Is.EqualTo(
                    reverseOrder ? new List<IDynamicLine> { secondDynamicLine, firstDynamicLine } :
                    new List<IDynamicLine> { firstDynamicLine, secondDynamicLine }
                )
            );
        }

        #region SpanAndLineRanges updates
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Add_New_TrackedNewCodeLines_For_Non_Excluded_New_Start_Lines(bool exclude)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;

            var mockNewDynamicLine = new Mock<IDynamicLine>();
            mockNewDynamicLine.SetupGet(l => l.Number).Returns(10);
            var newDynamicLine = mockNewDynamicLine.Object;
            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("text");
            mockTrackedNewCodeLine.SetupGet(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(newDynamicLine);
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var mockLineExcluder = new Mock<ILineExcluder>();
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("text")).Returns(exclude);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object,
                mockLineExcluder.Object
            );

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                textSnapshot,
                new List<SpanAndLineRange> { new SpanAndLineRange(new Span(), 1, 2), new SpanAndLineRange(new Span(), 1, 2) },
                null);

            if (exclude)
            {
                Assert.That(newCodeTracker.Lines, Is.Empty);
                Assert.That(changedLineNumbers, Is.Empty);
            }
            else
            {
                Assert.That(
                    newCodeTracker.Lines.Single(),
                    Is.SameAs(newDynamicLine)
                );

                Assert.That(changedLineNumbers.Single(), Is.EqualTo(1));
            }
        }

        [Test]
        public void Should_Not_Have_Changed_Lines_When_Line_Exists_And_Not_Updated_Or_Excluded()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("", 1, 1));
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("exclude");
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, new Mock<ILineExcluder>().Object, new List<int> { 1 }, textSnapshot);

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                currentTextSnapshot,
                new List<SpanAndLineRange> { new SpanAndLineRange(new Span(), 1, 1) },
                null);

            Assert.That(changedLineNumbers, Is.Empty);
            Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(dynamicLine));
        }

        [Test]
        public void Should_Have_Changed_Line_When_Line_Exists_And_Excluded()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("updated", 1, 1));
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.GetText(textSnapshot)).Returns("exclude");
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var mockLineExcluder = new Mock<ILineExcluder>();
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("updated")).Returns(true);
            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, mockLineExcluder.Object, new List<int> { 1 }, textSnapshot);

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                currentTextSnapshot,
                new List<SpanAndLineRange> { new SpanAndLineRange(new Span(), 1, 1) },
                null);

            Assert.That(changedLineNumbers, Is.EqualTo(new List<int> { 1 }));
            Assert.That(newCodeTracker.Lines, Is.Empty);
        }

        [Test]
        public void Should_Have_Old_And_New_Line_Numbers_When_Line_Number_Updated()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("updated", 2, 1));
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, new Mock<ILineExcluder>().Object, new List<int> { 1 }, textSnapshot);

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                currentTextSnapshot,
                new List<SpanAndLineRange> { },
                null);

            Assert.That(changedLineNumbers, Is.EqualTo(new List<int> { 1, 2 }));
            Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(dynamicLine));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Use_The_New_Line_Number_To_Reduce_Possible_New_Lines(bool newLineNumberReduces)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Update(currentTextSnapshot))
                .Returns(new TrackedNewCodeLineUpdate("updated", 2, 1));
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var newDynamicLine = new Mock<IDynamicLine>().Object;
            var newMockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            newMockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(newDynamicLine);
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(currentTextSnapshot, SpanTrackingMode.EdgeExclusive, 3)
            ).Returns(newMockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, new Mock<ILineExcluder>().Object, new List<int> { 1 }, textSnapshot);

            var potentialNewLineNumber = newLineNumberReduces ? 2 : 3;
            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                currentTextSnapshot,
                new List<SpanAndLineRange> { new SpanAndLineRange(new Span(), potentialNewLineNumber, potentialNewLineNumber) },
                null);

            if (newLineNumberReduces)
            {
                Assert.That(changedLineNumbers, Is.EqualTo(new List<int> { 1, 2 }));
                Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(dynamicLine));
            }
            else
            {
                Assert.That(changedLineNumbers, Is.EqualTo(new List<int> { 1, 2, 3 }));
                Assert.That(newCodeTracker.Lines.Count(), Is.EqualTo(2));
            }


        }
        #endregion

        #region NewCodeCodeRanges updates
        [Test]
        public void Should_Have_No_Changes_When_All_CodeSpanRange_Start_Line_Numbers_Are_Already_Tracked()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(1);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(mockDynamicLine.Object);
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, new Mock<ILineExcluder>().Object, new List<int> { 1 }, textSnapshot);

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                new Mock<ITextSnapshot>().Object,
                null,
                new List<CodeSpanRange> { new CodeSpanRange(1, 3) });

            Assert.That(changedLineNumbers, Is.Empty);
            Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(mockDynamicLine.Object));
        }

        [Test]
        public void Should_Have_Single_Changed_Line_Number_When_CodeSpanRange_Start_Line_Not_Tracked_And_No_Existing()
        {
            var currentTextSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = new Mock<IDynamicLine>().Object;
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(currentTextSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object, null);

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                currentTextSnapshot,
                null,
                new List<CodeSpanRange> { new CodeSpanRange(1, 3) });

            Assert.That(changedLineNumbers.Single(), Is.EqualTo(1));
            Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(dynamicLine));
        }

        private IDynamicLine CreateDynamicLine(int lineNumber)
        {
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(lineNumber);
            return mockDynamicLine.Object;
        }

        [Test]
        public void Should_Remove_Tracked_Lines_That_Are_Not_CodeSpanRange_Start()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;

            var mockTrackedNewCodeLine = new Mock<ITrackedNewCodeLine>();
            var dynamicLine = CreateDynamicLine(1);
            mockTrackedNewCodeLine.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(dynamicLine);
            var mockTrackedNewCodeLineFactory = new Mock<ITrackedNewCodeLineFactory>();
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 1)
            ).Returns(mockTrackedNewCodeLine.Object);
            var mockRemovedTrackedNewCodeLine2 = new Mock<ITrackedNewCodeLine>();
            var removedDynamicLine = CreateDynamicLine(2);
            mockRemovedTrackedNewCodeLine2.Setup(trackedNewCodeLine => trackedNewCodeLine.Line).Returns(removedDynamicLine);
            mockTrackedNewCodeLineFactory.Setup(trackedNewCodeLineFactory =>
                trackedNewCodeLineFactory.Create(textSnapshot, SpanTrackingMode.EdgeExclusive, 2)
            ).Returns(mockRemovedTrackedNewCodeLine2.Object);

            var newCodeTracker = new NewCodeTracker(
                mockTrackedNewCodeLineFactory.Object,
                new Mock<ILineExcluder>().Object,
                new List<int> { 1, 2, },
                textSnapshot);

            Assert.That(newCodeTracker.Lines.Count(), Is.EqualTo(2));

            var changedLineNumbers = newCodeTracker.GetChangedLineNumbers(
                new Mock<ITextSnapshot>().Object,
                null,
                new List<CodeSpanRange> { new CodeSpanRange(1, 3) });

            Assert.That(changedLineNumbers, Is.EqualTo(new List<int> { 2 }));
            Assert.That(newCodeTracker.Lines.Single(), Is.SameAs(dynamicLine));

        }
        #endregion
    }
}
