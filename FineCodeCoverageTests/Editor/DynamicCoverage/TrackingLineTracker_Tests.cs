using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class  TrackingLineTracker_Tests
    {
        [Test]
        public void Should_Have_Single_TrackingLine_Line()
        {
            var mockTrackingLine = new Mock<ITrackingLine>();
            var line = new Mock<IDynamicLine>().Object;
            mockTrackingLine.Setup(t => t.Line).Returns(line);
            var trackingLineTracker = new TrackingLineTracker(mockTrackingLine.Object,ContainingCodeTrackerType.OtherLines);

            Assert.That(trackingLineTracker.Lines.Single(), Is.SameAs(line));
        }

        [Test]
        public void Should_Update_The_TrackingLine_When_No_Empty()
        {
            var autoMoqer = new AutoMoqer();
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockTrackingLine = autoMoqer.GetMock<ITrackingLine>();
            var updatedLines = new List<int> { 10, 11 };
            mockTrackingLine.Setup(trackingLine => trackingLine.Update(textSnapshot)).Returns(updatedLines);

            var trackingLineTracker = new TrackingLineTracker(mockTrackingLine.Object, ContainingCodeTrackerType.OtherLines);

            var updatedLineNumbers = trackingLineTracker.GetUpdatedLineNumbers(null, textSnapshot, null);

            Assert.That(updatedLineNumbers, Is.SameAs(updatedLines));
        }

        [TestCase(ContainingCodeTrackerType.NotIncluded)]
        [TestCase(ContainingCodeTrackerType.CoverageLines)]
        public void Should_Have_Correct_ContainingCodeTrackerType(ContainingCodeTrackerType containingCodeTrackerType)
        {
            var autoMoqer = new AutoMoqer();
            var notIncludedCodeTracker = new TrackingLineTracker(null, containingCodeTrackerType);
            Assert.That(notIncludedCodeTracker.Type, Is.EqualTo(containingCodeTrackerType));
        }
    }
    

}
