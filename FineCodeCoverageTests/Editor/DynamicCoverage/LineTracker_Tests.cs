using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class LineTracker_Tests
    {
        private Mock<ITextSnapshot> GetMockTextSnapshot()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(t => t.Length).Returns(100);
            return mockTextSnapshot;
        }

        [TestCase(SpanTrackingMode.EdgePositive)]
        [TestCase(SpanTrackingMode.EdgeInclusive)]
        public void Should_Create_EdgeExclusive_Tracking_Span_With_The_Extent_Of_The_Line(SpanTrackingMode spanTrackingMode)
        {
            var mockTextSnapshot = GetMockTextSnapshot();
            var mockTextSnapshotLine = new Mock<ITextSnapshotLine>();
            var lineExtent = new SnapshotSpan(mockTextSnapshot.Object, 10, 20);
            mockTextSnapshotLine.SetupGet(l => l.Extent).Returns(lineExtent);
            mockTextSnapshot.Setup(t => t.GetLineFromLineNumber(1)).Returns(mockTextSnapshotLine.Object);

            var trackingLineFactory = new LineTracker();
            var trackingSpan = trackingLineFactory.CreateTrackingSpan(mockTextSnapshot.Object, 1,spanTrackingMode);
            
            mockTextSnapshot.Verify(t => t.CreateTrackingSpan(new SnapshotSpan(mockTextSnapshot.Object, 10, 20), spanTrackingMode));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_GetLineNumber_From_Start_Or_End(bool fromEnd)
        {
            var mockTrackingSpan = new Mock<ITrackingSpan>();
            var mockTextSnapshot = GetMockTextSnapshot();
            var endPoint = new SnapshotPoint(mockTextSnapshot.Object, 50);
            mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetEndPoint(mockTextSnapshot.Object))
                .Returns(endPoint);
            var startPoint = new SnapshotPoint(mockTextSnapshot.Object, 10);
            mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetStartPoint(mockTextSnapshot.Object))
                .Returns(startPoint);

            mockTextSnapshot.Setup(snapshot => snapshot.GetLineNumberFromPosition(endPoint)).Returns(5);
            mockTextSnapshot.Setup(snapshot => snapshot.GetLineNumberFromPosition(startPoint)).Returns(1);
            var lineNumber = new LineTracker().GetLineNumber(mockTrackingSpan.Object, mockTextSnapshot.Object, fromEnd);

            Assert.That(lineNumber, Is.EqualTo(fromEnd ? 5 : 1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Get_Line_Number_And_Text_From_Start_Or_End(bool fromEnd)
        {
            (ITextSnapshotLine, SnapshotSpan) CreateTextSnapshotLineAndExtent(int lineNumber, ITextSnapshot textSnapshot)
            {
                var mockTextSnapshotLine = new Mock<ITextSnapshotLine>();
                mockTextSnapshotLine.SetupGet(l => l.LineNumber).Returns(lineNumber);
                var extent = new SnapshotSpan(textSnapshot, 10, 20 + lineNumber);
                mockTextSnapshotLine.SetupGet(l => l.Extent).Returns(extent);
                return (mockTextSnapshotLine.Object, extent);
            }

            var mockTrackingSpan = new Mock<ITrackingSpan>();
            var mockTextSnapshot = GetMockTextSnapshot();
            var endPoint = new SnapshotPoint(mockTextSnapshot.Object, 50);
            mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetEndPoint(mockTextSnapshot.Object))
                .Returns(endPoint);
            var startPoint = new SnapshotPoint(mockTextSnapshot.Object, 10);
            mockTrackingSpan.Setup(trackingSpan => trackingSpan.GetStartPoint(mockTextSnapshot.Object))
                .Returns(startPoint);

            var (endLine, endExtent) = CreateTextSnapshotLineAndExtent(5, mockTextSnapshot.Object);
            var (startLine, startExtent) = CreateTextSnapshotLineAndExtent(1, mockTextSnapshot.Object);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(endPoint)).Returns(endLine);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetText(endExtent)).Returns("EndLineText");
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineFromPosition(startPoint)).Returns(startLine);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetText(startExtent)).Returns("StartLineText");

            var info = new LineTracker().GetTrackedLineInfo(mockTrackingSpan.Object, mockTextSnapshot.Object, fromEnd);
            
            Assert.That(info.LineNumber, Is.EqualTo(fromEnd ? 5 : 1));
            Assert.That(info.LineText, Is.EqualTo(fromEnd ? "EndLineText" : "StartLineText"));
        }
    }
}
