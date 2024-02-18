using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class ContainingCodeTracker_Tests
    {
        [Test]
        public void Should_Process_TrackingSpanRange_Changes()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0,1),0,0)};

            var autoMoqer = new AutoMoqer();
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup( trackingSpanRange => trackingSpanRange.Process(textSnapshot,spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(spanChanges,false,false));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();
            
            containingCodeTracker.ProcessChanges(textSnapshot,spanChanges);

            mockTrackingSpanRange.VerifyAll();
        }

        [Test]
        public void Should_Return_The_Non_Intersecting_Spans_In_The_Result()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };

            var autoMoqer = new AutoMoqer();
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            var nonInterectingSpans = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(50, 10), 5, 6) };
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(nonInterectingSpans, true, false));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            var processResult = containingCodeTracker.ProcessChanges(textSnapshot, spanChanges);

            Assert.That(processResult.UnprocessedSpans, Is.SameAs(nonInterectingSpans));
        }

        [Test]
        public void Should_Return_Empty_Changed_ContainingCodeTrackerProcessResult_When_TrackingSpanRange_Is_Empty()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };

            var autoMoqer = new AutoMoqer();
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(spanChanges, true, false));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            var processResult = containingCodeTracker.ProcessChanges(textSnapshot, spanChanges);

            Assert.That(processResult.Changed, Is.True);
            Assert.That(processResult.IsEmpty, Is.True);
        }

        [Test]
        public void Should_Return_Lines_From_TrackedCoverageLines_When_No_DirtyLine()
        {
            var autoMoqer = new AutoMoqer();
            var trackedLines = new List<IDynamicLine> { new Mock<IDynamicLine>().Object };
            autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines)
                .Returns(trackedLines);
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            Assert.That(trackedLines, Is.SameAs(containingCodeTracker.Lines));

        }

        [TestCase(true,true,true,true)]
        [TestCase(false, true, true, false)]
        [TestCase(true, false, true, false)]
        [TestCase(true, true,false, false)]
        public void Should_Create_A_Dirty_Line_From_The_First_Tracking_Span_When_Intersected_TextChanged_And_There_Are_Coverage_Lines(
            bool intersected,
            bool textChanged,
            bool areCoverageLines,
            bool expectedChanged
        )
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };

            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines)
                .Returns(areCoverageLines ? new List<IDynamicLine> { new Mock<IDynamicLine>().Object } : Enumerable.Empty<IDynamicLine>());
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            var firstTrackingSpan = new Mock<ITrackingSpan>().Object;
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.GetFirstTrackingSpan()).Returns(firstTrackingSpan);
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(intersected ? Enumerable.Empty<SpanAndLineRange>().ToList() : spanChanges, false, textChanged));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            containingCodeTracker.ProcessChanges(textSnapshot, spanChanges);

            autoMoqer.Verify<IDirtyLineFactory>(dirtyLineFactory => dirtyLineFactory.Create(firstTrackingSpan, textSnapshot),ExpectedTimes(expectedChanged));
        }

        private (ContainingCodeTracker, IDynamicLine,IContainingCodeTrackerProcessResult,Mock<IDirtyLine>,Mock<ITrackingSpanRange>) CreateDirtyLine()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };

            var autoMoqer = new AutoMoqer();
            var mockDirtyLine = new Mock<IDirtyLine>();
            var dirtyLineLine = new Mock<IDynamicLine>().Object;
            mockDirtyLine.SetupGet(dirtyLine => dirtyLine.Line).Returns(dirtyLineLine);
            autoMoqer.Setup<IDirtyLineFactory, IDirtyLine>(dirtyLineFactory => dirtyLineFactory.Create(It.IsAny<ITrackingSpan>(), It.IsAny<ITextSnapshot>()))
                .Returns(mockDirtyLine.Object);
            autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines)
                .Returns(new List<IDynamicLine> { new Mock<IDynamicLine>().Object });
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(Enumerable.Empty<SpanAndLineRange>().ToList(), false, true));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            var result = containingCodeTracker.ProcessChanges(textSnapshot, spanChanges);
            return (containingCodeTracker, dirtyLineLine, result,mockDirtyLine,mockTrackingSpanRange);
        }

        [Test]
        public void Should_Be_Changed_When_Dirty_Line_Is_Created()
        {
            var (_, _, result,_,_) = CreateDirtyLine();
            Assert.That(result.Changed, Is.True);
        }

        [Test]
        public void Should_Return_The_Dirty_Line_If_Created()
        {
            var (containingCodeTracker, dirtyLineLine, _,_,_) = CreateDirtyLine();
            
            Assert.That(containingCodeTracker.Lines, Is.EqualTo(new List<IDynamicLine> { dirtyLineLine }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_Dirty_Line_With_TextSnapshot_When_Changed(bool dirtyLineChanged)
        {
            var (containingCodeTracker, dirtyLineLine, _, mockDirtyLine,mockTrackingSpanRange) = CreateDirtyLine();
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            mockDirtyLine.Setup(dirtyLine => dirtyLine.Update(textSnapshot)).Returns(dirtyLineChanged);
            var changes = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot,changes))
                .Returns(new TrackingSpanRangeProcessResult(Enumerable.Empty<SpanAndLineRange>().ToList(), false, true));
            
            var result = containingCodeTracker.ProcessChanges(textSnapshot, changes);

            Assert.That(result.Changed, Is.EqualTo(dirtyLineChanged));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_TrackedCoverageLines_When_Not_Dirty(bool trackedCoverageLinesChanged)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var spanChanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(0, 1), 0, 0) };

            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<ITrackedCoverageLines, bool>(trackedCoverageLines => trackedCoverageLines.Update(textSnapshot)).Returns(trackedCoverageLinesChanged);
            var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, spanChanges))
                .Returns(new TrackingSpanRangeProcessResult(spanChanges, false, false));
            var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

            var result = containingCodeTracker.ProcessChanges(textSnapshot, spanChanges);

            autoMoqer.Verify<ITrackedCoverageLines>(trackedCoverageLines => trackedCoverageLines.Update(textSnapshot));
            Assert.That(result.Changed, Is.EqualTo(trackedCoverageLinesChanged));
        }

        private static Times ExpectedTimes(bool expected) => expected ? Times.Once() : Times.Never();
    }
    //internal class ContainingCodeTracker_Tests
    //{
    //    [Test]
    //    public void Should_Return_Lines_From_TrackedCoverageLines()
    //    {
    //        var autoMoqer = new AutoMoqer();

    //        var lines = new List<IDynamicLine> { };
    //        autoMoqer.Setup<ITrackedCoverageLines, IEnumerable<IDynamicLine>>(trackedCoverageLines => trackedCoverageLines.Lines).Returns(lines);
            
    //        var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

    //        Assert.That(containingCodeTracker.Lines, Is.SameAs(lines));
    //    }

    //    [TestCase(true)]
    //    [TestCase(false)]
    //    public void Should_Dirty_The_TrackedCoverageLines_And_Be_Changed_When_TrackingSpanRange_IntersectsWith(bool intersectsWith)
    //    {
    //        throw new System.NotImplementedException();
    //        //var autoMoqer = new AutoMoqer();
            
    //        //var currentSnapshot = new Mock<ITextSnapshot>().Object;
    //        //var newSpanChanges = new List<Span> { new Span(0, 1) };

    //        //var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
    //        //mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.GetNonIntersecting(currentSnapshot, newSpanChanges)).Returns(intersectsWith);
    //        //var mockTrackedCoverageLines = autoMoqer.GetMock<ITrackedCoverageLines>();

    //        //var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

    //        //var changed = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

    //        //Assert.That(changed, Is.EqualTo(intersectsWith));
    //        //mockTrackedCoverageLines.Verify(trackedCoverageLines => trackedCoverageLines.Dirty(), intersectsWith? Times.Once():Times.Never());
    //    }

    //    [Test]
    //    public void Should_Call_TrackingSpanRange_IntersectsWith_No_More_Once_Dirty()
    //    {
    //        //var autoMoqer = new AutoMoqer();

    //        //var currentSnapshot = new Mock<ITextSnapshot>().Object;
    //        //var newSpanChanges = new List<Span> { new Span(0, 1) };

    //        //var mockTrackingSpanRange = autoMoqer.GetMock<ITrackingSpanRange>();
    //        //mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.GetNonIntersecting(currentSnapshot, newSpanChanges)).Returns(true);
    //        //var mockTrackedCoverageLines = autoMoqer.GetMock<ITrackedCoverageLines>();

    //        //var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();

    //        //containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);
    //        //containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

    //        //Assert.That(mockTrackingSpanRange.Invocations.Count, Is.EqualTo(1));
    //        throw new System.NotImplementedException();
    //    }

    //    [Test]
    //    public void Should_Not_Throw_If_No_ITrackingSpanRange()
    //    {
    //        //var containingCodeTracker = new ContainingCodeTracker(new Mock<ITrackedCoverageLines>().Object);

    //        //var currentSnapshot = new Mock<ITextSnapshot>().Object;
    //        //var newSpanChanges = new List<Span> { new Span(0, 1) };

    //        //var changes = containingCodeTracker.ProcessChanges(currentSnapshot, newSpanChanges);

    //        //Assert.False(changes);
    //        throw new System.NotImplementedException();
    //    }

    //    [TestCase(true)]
    //    [TestCase(false)]
    //    public void Should_Be_Changed_When_Updates_TrackedCoverageLines_Have_Changed(bool trackedCoverageLinesChanged)
    //    {
    //        throw new System.NotImplementedException();
    //        //var autoMoqer = new AutoMoqer();
    //        //var currentSnapshot = new Mock<ITextSnapshot>().Object;
    //        //var lines = new List<IDynamicLine> { };
    //        //autoMoqer.Setup<ITrackedCoverageLines, bool>(x => x.Update(currentSnapshot)).Returns(trackedCoverageLinesChanged);

    //        //var containingCodeTracker = autoMoqer.Create<ContainingCodeTracker>();
            
    //        //var changed = containingCodeTracker.ProcessChanges(currentSnapshot, new List<Span> { new Span(0, 1) });

    //        //Assert.That(changed, Is.EqualTo(trackedCoverageLinesChanged));
    //    }
    //}

}
