using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackedNewCodeLine_Tests
    {
        [Test]
        public void Should_Have_Line_With_Coverage_Type_NewLine_And_Adjusted_Line_Number()
        {
            var line = new TrackedNewCodeLine(new Mock<ITrackingSpan>().Object, 10, new Mock<ILineTracker>().Object).Line;

            Assert.That(line.CoverageType, Is.EqualTo(DynamicCoverageType.NewLine));
            Assert.That(line.Number, Is.EqualTo(11));
        }

        [Test]
        public void Should_Delegate_GetText_To_LineTracker()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;
            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetTrackedLineInfo(trackingSpan, textSnapshot, true))
                .Returns(new TrackedLineInfo(10, "line text"));

            var trackedNewCodeLine = new TrackedNewCodeLine(trackingSpan, 10, mockLineTracker.Object);

            Assert.That(trackedNewCodeLine.GetText(textSnapshot), Is.EqualTo("line text"));
        }

        private (TrackedNewCodeLineUpdate, IDynamicLine) Update(int startLineNumber,int newLineNumber)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var trackingSpan = new Mock<ITrackingSpan>().Object;
            var mockLineTracker = new Mock<ILineTracker>();
            mockLineTracker.Setup(lineTracker => lineTracker.GetTrackedLineInfo(trackingSpan, textSnapshot, true))
                .Returns(new TrackedLineInfo(newLineNumber, "line text"));

            var trackedNewCodeLine = new TrackedNewCodeLine(trackingSpan, startLineNumber, mockLineTracker.Object);

            return (trackedNewCodeLine.Update(textSnapshot), trackedNewCodeLine.Line);
        }

        [Test]
        public void Should_Update_Line_Number_If_Changed()
        {
            var (_, line) = Update(10, 20);

            Assert.That(line.Number, Is.EqualTo(21));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Changed_When_Line_Number_Changes(bool changeLineNumber)
        {
            var (update, _) = Update(10, changeLineNumber ? 20 : 10);

            Assert.That(update.LineUpdated, Is.EqualTo(changeLineNumber));
        }

        [Test]
        public void Should_Return_Delegated_Line_Text()
        {
            var (update, _) = Update(10, 10);

            Assert.That(update.Text, Is.EqualTo("line text"));
        }
    }
}
