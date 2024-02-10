using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class ContainingCodeTracker_Tests
    {
        [Test]
        public void Should_Return_Lines_From_TrackedCoverageLines()
        {
            var autoMoqer = new AutoMoqer();

            var lines = new List<IDynamicLine> { };
            autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines).Returns(lines);
            
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            Assert.That(containingCodeTracker.Lines, Is.SameAs(lines));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Dirty_The_TrackedCoverageLines_And_Be_Changed_When_TrackingSpanRange_IntersectsWith(bool intersectsWith)
        {
            var autoMoqer = new AutoMoqer();
            
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanChanges = new List<Span> { new Span(0, 1) };

            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.IntersectsWith(currentSnapshot, newSpanChanges)).Returns(intersectsWith);
            var mockTrackedCoverageLines = autoMoqer.GetMock<ITrackedCoverageLines>();

            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            var changed = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

            Assert.That(changed, Is.EqualTo(intersectsWith));
            mockTrackedCoverageLines.Verify(trackedCoverageLines => trackedCoverageLines.Dirty(), intersectsWith? Times.Once():Times.Never());
        }

        [Test]
        public void Should_Call_TrackingSpanRange_IntersectsWith_No_More_Once_Dirty()
        {
            var autoMoqer = new AutoMoqer();

            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanChanges = new List<Span> { new Span(0, 1) };

            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.IntersectsWith(currentSnapshot, newSpanChanges)).Returns(true);
            var mockTrackedCoverageLines = autoMoqer.GetMock<ITrackedCoverageLines>();

            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
            containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

            Assert.That(mockTrackingSpanRange.Invocations.Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_Not_Throw_If_No_ITrackingSpanRange()
        {
            var containingCodeTracker = new ContainingCodeTracker(new Mock<ITrackedCoverageLines>().Object);
            
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanChanges = new List<Span> { new Span(0, 1) };

            var changes = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

            Assert.False(changes);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Be_Changed_When_Updates_TrackedCoverageLines_Have_Changed(bool trackedCoverageLinesChanged)
        {
            var autoMoqer = new AutoMoqer();
            var currentSnapshot = new Mock<ITextSnapshot>().Object;
            var lines = new List<IDynamicLine> { };
            autoMoqer.Setup<ITrackedCoverageLines, bool>(x => x.Update(currentSnapshot)).Returns(trackedCoverageLinesChanged);

            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();
            
            var changed = containingCodeTracker.ProcessChanges(currentSnapshot, new List<Span> { new Span(0, 1) });

            Assert.That(changed, Is.EqualTo(trackedCoverageLinesChanged));
        }
    }
}
