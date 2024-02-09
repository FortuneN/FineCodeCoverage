using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Coverage_Colours
{
    internal class TrackingSpanRange_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_True_If_Any_Intersect_With_Changes(bool intersects)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(ts => ts.Length).Returns(1000);
            var textSnapshot = mockTextSnapshot.Object;

            var mockTrackingSpan1 = new Mock<ITrackingSpan>();
            mockTrackingSpan1.Setup(trackingSpan => trackingSpan.GetSpan(textSnapshot)).Returns(new SnapshotSpan(textSnapshot, new Span(0, 1)));
            var mockTrackingSpan2 = new Mock<ITrackingSpan>();
            mockTrackingSpan2.Setup(trackingSpan => trackingSpan.GetSpan(textSnapshot)).Returns(new SnapshotSpan(textSnapshot, new Span(10, 10)));
            var trackingSpans = new List<ITrackingSpan> { mockTrackingSpan1.Object, mockTrackingSpan2.Object };

            var trackingSpanRange = new TrackingSpanRange(trackingSpans);

            var possiblyIntersecting = intersects ? new Span(15, 1) : new Span(100, 200);
            var newSpanChanges = new List<Span> { new Span(5, 1), possiblyIntersecting};
            Assert.That(trackingSpanRange.IntersectsWith(textSnapshot, newSpanChanges), Is.EqualTo(intersects));
            
        }
    }
}
