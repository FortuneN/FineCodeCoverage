using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackingSpanRange_Tests
    {
        [Test]
        public void Should_Return_Spans_That_Do_Not_Intersect()
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();
            //mockTextSnapshot.SetupGet(ts => ts.Length).Returns(1000);
            //var textSnapshot = mockTextSnapshot.Object;

            //var mockTrackingSpan1 = new Mock<ITrackingSpan>();
            //mockTrackingSpan1.Setup(trackingSpan => trackingSpan.GetSpan(textSnapshot))
            //    .Returns(new SnapshotSpan(textSnapshot, new Span(10, 5)));
            //var mockTrackingSpan2 = new Mock<ITrackingSpan>();
            //mockTrackingSpan2.Setup(trackingSpan => trackingSpan.GetSpan(textSnapshot))
            //    .Returns(new SnapshotSpan(textSnapshot, new Span(20, 10)));
            //var trackingSpans = new List<ITrackingSpan> { mockTrackingSpan1.Object, mockTrackingSpan2.Object };

            //var trackingSpanRange = new TrackingSpanRange(trackingSpans);
            
            //var newSpanChanges = new List<Span> { new Span(5, 1), new Span(11,5), new Span(25, 5), new Span(35, 5) };
            //var nonIntersecting = trackingSpanRange.GetNonIntersecting(textSnapshot, newSpanChanges);

            //Assert.That(nonIntersecting, Is.EqualTo(new List<Span> { new Span(5, 1), new Span(35, 5) }));
        }

        [Test]
        public void Should_Stop_Tracking_When_Empty()
        {
            throw new System.NotImplementedException();
            //var mockTextSnapshot = new Mock<ITextSnapshot>();
            //mockTextSnapshot.SetupGet(ts => ts.Length).Returns(1000);
            //var textSnapshot = mockTextSnapshot.Object;

            //var mockTrackingSpan1 = new Mock<ITrackingSpan>(MockBehavior.Strict);
            //mockTrackingSpan1.Setup(trackingSpan => trackingSpan.GetSpan(textSnapshot))
            //    .Returns(new SnapshotSpan(textSnapshot, new Span(11, 0)));
            //var trackingSpans = new List<ITrackingSpan> { mockTrackingSpan1.Object};

            //var trackingSpanRange = new TrackingSpanRange(trackingSpans);

            //var newSpanChanges = new List<Span> { new Span(11, 0) };
            //var nonIntersecting = trackingSpanRange.GetNonIntersecting(textSnapshot, newSpanChanges);
            //Assert.That(nonIntersecting, Has.Count.EqualTo(0));
            //nonIntersecting = trackingSpanRange.GetNonIntersecting(textSnapshot, newSpanChanges);
            //Assert.That(nonIntersecting, Has.Count.EqualTo(1));

        }
    }
}
