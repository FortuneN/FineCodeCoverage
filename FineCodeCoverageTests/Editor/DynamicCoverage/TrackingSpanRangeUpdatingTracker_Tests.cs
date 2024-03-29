﻿using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TrackingSpanRangeUpdatingTracker_Tests
    {
        [Test]
        public void Should_Get_Lines_From_IUpdatableDynamicLines()
        {
            var mockUpdatableDynamicLines = new Mock<IUpdatableDynamicLines>();
            var dynamicLines = Enumerable.Empty<IDynamicLine>();
            mockUpdatableDynamicLines.SetupGet(updatableDynamicLines => updatableDynamicLines.Lines).Returns(dynamicLines);

            var trackingSpanRangeUpdatingTracker = new TrackingSpanRangeUpdatingTracker(null, mockUpdatableDynamicLines.Object);

            Assert.That(trackingSpanRangeUpdatingTracker.Lines, Is.SameAs(dynamicLines));
        }

        private static IDynamicLine CreateDynamicLine(int lineNumber)
        {
            var mockDynamicLine = new Mock<IDynamicLine>();
            mockDynamicLine.SetupGet(dynamicLine => dynamicLine.Number).Returns(lineNumber);
            return mockDynamicLine.Object;
        }

        [Test]
        public void Should_Not_Update_IUpdatableDynamicLines_When_Empty_Returning_All_UpdatableDynamicLines_Line_Numbers()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanAndLineRanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(1, 2), 0, 0) };
            var mockTrackingSpanRange = new Mock<ITrackingSpanRange>();
            var nonIntersectingSpans = new List<SpanAndLineRange>();
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, newSpanAndLineRanges))
                .Returns(new TrackingSpanRangeProcessResult(mockTrackingSpanRange.Object, nonIntersectingSpans, true, false));
            var mockUpdatableDynamicLines = new Mock<IUpdatableDynamicLines>(MockBehavior.Strict);

            var dynamicLines = new List<IDynamicLine> { CreateDynamicLine(1), CreateDynamicLine(2) };
            mockUpdatableDynamicLines.SetupGet(updatableDynamicLines => updatableDynamicLines.Lines).Returns(dynamicLines);

            var trackingSpanRangeUpdatingTracker = new TrackingSpanRangeUpdatingTracker(mockTrackingSpanRange.Object, mockUpdatableDynamicLines.Object);

            var result = trackingSpanRangeUpdatingTracker.ProcessChanges(textSnapshot, newSpanAndLineRanges);

            Assert.That(result.UnprocessedSpans, Is.SameAs(nonIntersectingSpans));
            Assert.That(result.ChangedLines, Is.EqualTo(new List<int> { 1,2}));
            Assert.That(result.IsEmpty, Is.True);
        }

        [Test]
        public void Should_Update_IUpdatableDynamicLines_When_Non_Empty()
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var newSpanAndLineRanges = new List<SpanAndLineRange> { new SpanAndLineRange(new Span(1, 2), 0, 0) };
            var mockTrackingSpanRange = new Mock<ITrackingSpanRange>();
            var nonIntersectingSpans = new List<SpanAndLineRange>();
            var trackingSpanRangeProcessResult = new TrackingSpanRangeProcessResult(mockTrackingSpanRange.Object, nonIntersectingSpans, false, false);
            mockTrackingSpanRange.Setup(trackingSpanRange => trackingSpanRange.Process(textSnapshot, newSpanAndLineRanges))
                .Returns(trackingSpanRangeProcessResult);
            var mockUpdatableDynamicLines = new Mock<IUpdatableDynamicLines>();
            var updatedLineNumbers = new List<int> { 1, 2 };
            mockUpdatableDynamicLines.Setup(
                updatableDynamicLines => updatableDynamicLines.GetUpdatedLineNumbers(trackingSpanRangeProcessResult, textSnapshot, newSpanAndLineRanges)
            ).Returns(updatedLineNumbers);

            var trackingSpanRangeUpdatingTracker = new TrackingSpanRangeUpdatingTracker(mockTrackingSpanRange.Object, mockUpdatableDynamicLines.Object);

            var result = trackingSpanRangeUpdatingTracker.ProcessChanges(textSnapshot, newSpanAndLineRanges);

            Assert.That(result.UnprocessedSpans, Is.SameAs(nonIntersectingSpans));
            Assert.That(result.ChangedLines, Is.SameAs(updatedLineNumbers));
            Assert.That(result.IsEmpty, Is.False);
        }

        [TestCase(ContainingCodeTrackerType.NotIncluded)]
        [TestCase(ContainingCodeTrackerType.OtherLines)]
        public void Should_GetState(ContainingCodeTrackerType containingCodeTrackerType)
        {
            var autoMoqer = new AutoMoqer();
            var mockUpdatableDynamicLines = autoMoqer.GetMock<IUpdatableDynamicLines>();
            mockUpdatableDynamicLines.SetupGet(updatableDynamicLines => updatableDynamicLines.Type).Returns(containingCodeTrackerType);
            var lines = Enumerable.Empty<IDynamicLine>();
            mockUpdatableDynamicLines.SetupGet(updatableDynamicLines => updatableDynamicLines.Lines).Returns(lines);
            var codeSpanRange = new CodeSpanRange(1, 2);
            autoMoqer.Setup<ITrackingSpanRange, CodeSpanRange>(trackingSpanRange => trackingSpanRange.ToCodeSpanRange()).Returns(codeSpanRange);
            var trackingSpanRangeUpdatingTracker = autoMoqer.Create<TrackingSpanRangeUpdatingTracker>();

            var state = trackingSpanRangeUpdatingTracker.GetState();

            Assert.That(containingCodeTrackerType, Is.EqualTo(state.Type));
            Assert.That(lines, Is.SameAs(state.Lines));
            Assert.That(codeSpanRange, Is.SameAs(state.CodeSpanRange));

        }
    }
    

}
