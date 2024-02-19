using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackingSpanRange_Tests
    {
        private (TrackingSpanRange,Mock<ITrackingSpan>, Mock<ITrackingSpan>) CreateTrackingSpanRange(string firstText = "")
        {
            var mockFirstSnapshot = new Mock<ITextSnapshot>();
            mockFirstSnapshot.Setup(firstSnapshot => firstSnapshot.GetText(It.IsAny<Span>())).Returns(firstText);
                
            var mockStartTrackingSpan = new Mock<ITrackingSpan>();
            mockStartTrackingSpan.Setup(startTrackingspan => startTrackingspan.GetSpan(It.IsAny<ITextSnapshot>()))
                .Returns(new SnapshotSpan(mockFirstSnapshot.Object, new Span()));
            var mockEndTrackingSpan = new Mock<ITrackingSpan>();
            mockEndTrackingSpan.Setup(startTrackingspan => startTrackingspan.GetSpan(It.IsAny<ITextSnapshot>()))
                .Returns(new SnapshotSpan(mockFirstSnapshot.Object, new Span()));
            return (new TrackingSpanRange(mockStartTrackingSpan.Object, mockEndTrackingSpan.Object, mockFirstSnapshot.Object), mockStartTrackingSpan, mockEndTrackingSpan);
        }

        [Test]
        public void Should_Return_NonIntersecting_By_Range_Line_Numbers()
        {
            var (trackingSpanRange,mockFirstTrackingSpan, mockEndTrackingSpan) = CreateTrackingSpanRange();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(ts => ts.Length).Returns(1000);
            void SetupSpan(Mock<ITrackingSpan> mockTrackingSpan,int end,int lineNumber)
            {
                mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetSpan(mockTextSnapshot.Object))
                    .Returns(new SnapshotSpan(mockTextSnapshot.Object, new Span(0, end)));
                mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(end)).Returns(lineNumber);
            }

            // setting the range to lines between 5 and 10
            SetupSpan(mockFirstTrackingSpan,10, 5);
            SetupSpan(mockEndTrackingSpan,20, 10);

            var expectedNewSpanAndLineRanges = new List<SpanAndLineRange>
            {
                new SpanAndLineRange(new Span(0,1),0,0),
                new SpanAndLineRange(new Span(160,10),11,11),
            };

            var newSpanAndLineRanges = new List<SpanAndLineRange>
            {
                expectedNewSpanAndLineRanges[0],

                new SpanAndLineRange(new Span(50,10),4,5),
                new SpanAndLineRange(new Span(100,10),5,6),
                new SpanAndLineRange(new Span(110,10),7,7),
                new SpanAndLineRange(new Span(120,10),8,8),
                new SpanAndLineRange(new Span(130,10),9,9),
                new SpanAndLineRange(new Span(140,10),10,10),
                new SpanAndLineRange(new Span(150,10),10,11),

                expectedNewSpanAndLineRanges[1]

            };

            var result = trackingSpanRange.Process(mockTextSnapshot.Object, newSpanAndLineRanges);

            Assert.That(expectedNewSpanAndLineRanges, Is.EqualTo(result.NonIntersectingSpans));
        }

        private TrackingSpanRangeProcessResult TextTest(string firstText, string changeText)
        {
            var (trackingSpanRange, mockFirstTrackingSpan, mockEndTrackingSpan) = CreateTrackingSpanRange(firstText);
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            var firstStart = 10;
            var endEnd = 50;
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetText(new Span(firstStart, endEnd - firstStart))).Returns(changeText);
            mockTextSnapshot.SetupGet(ts => ts.Length).Returns(1000);
            void SetupSpan(Mock<ITrackingSpan> mockTrackingSpan, int start, int end)
            {
                mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetSpan(mockTextSnapshot.Object))
                    .Returns(new SnapshotSpan(mockTextSnapshot.Object, new Span(start, end - start)));
            }
            SetupSpan(mockFirstTrackingSpan, firstStart, 15);
            SetupSpan(mockEndTrackingSpan, 20, endEnd);

           return trackingSpanRange.Process(mockTextSnapshot.Object, new List<SpanAndLineRange>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_TextChanged_When_TextChanged_For_The_Range(bool changeText)
        {
            var result = TextTest("range text", changeText ? "new" : "range text");

            Assert.That(result.TextChanged, Is.EqualTo(changeText));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_Empty_When_Range_Text_IsNullOrWhitespace(bool isEmpty)
        {
            var result = TextTest("", isEmpty ? "         " : "   range text  ");
            Assert.That(result.IsEmpty, Is.EqualTo(isEmpty));
        }

        [Test]
        public void Should_GetFirstTrackingSpan()
        {
            var (trackingSpanRange,mockFirstTrackingSpan, _) = CreateTrackingSpanRange();

            Assert.That(mockFirstTrackingSpan.Object, Is.SameAs(trackingSpanRange.GetFirstTrackingSpan()));
        }
    }
}
