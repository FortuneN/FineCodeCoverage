using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackingLineFactory_Tests
    {
        [Test]
        public void Should_Create_EdgeExclusive_Tracking_Span_With_The_Extent_Of_The_Line()
        {
            var autoMoqer = new AutoMoqer();
            var mockTextSnapshot = autoMoqer.GetMock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(t => t.Length).Returns(100);
            var mockTextSnapshotLine = autoMoqer.GetMock<ITextSnapshotLine>();
            var lineExtent = new SnapshotSpan(mockTextSnapshot.Object,10,20);
            mockTextSnapshotLine.SetupGet(l => l.Extent).Returns(lineExtent);
            mockTextSnapshot.Setup(t => t.GetLineFromLineNumber(1)).Returns(mockTextSnapshotLine.Object);
            var trackingLineFactory = autoMoqer.Create<TrackingLineFactory>();
            var trackingSpan = trackingLineFactory.Create(mockTextSnapshot.Object, 1);
            mockTextSnapshot.Verify(t => t.CreateTrackingSpan(new SnapshotSpan(mockTextSnapshot.Object, 10, 20), SpanTrackingMode.EdgeExclusive));
        }
    }
}
