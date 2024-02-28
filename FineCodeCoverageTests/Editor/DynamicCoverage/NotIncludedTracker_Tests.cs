using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Linq;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class  NotIncludedTracker_Tests
    {
        [Test]
        public void Should_Have_Single_TrackingLine_Line()
        {
            var mockTrackingLine = new Mock<ITrackingLine>();
            var line = new Mock<IDynamicLine>().Object;
            mockTrackingLine.Setup(t => t.Line).Returns(line);
            var notIncludedCodeTracker = new NotIncludedCodeTracker(mockTrackingLine.Object);

            Assert.That(notIncludedCodeTracker.Lines.Single(), Is.SameAs(line));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_The_TrackingLine_When_No_Empty(bool lineChanged)
        {
            var autoMoqer = new AutoMoqer();
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockTrackingLine = autoMoqer.GetMock<ITrackingLine>();
            mockTrackingLine.Setup(t => t.Update(textSnapshot)).Returns(lineChanged);

            var notIncludedCodeTracker = autoMoqer.Create<NotIncludedCodeTracker>();

            var updated = notIncludedCodeTracker.Update(null, textSnapshot, null);

            Assert.That(lineChanged, Is.EqualTo(updated));
        }

        [Test]
        public void Should_Have_Correct_ContainingCodeTrackerType()
        {
            var autoMoqer = new AutoMoqer();
            var notIncludedCodeTracker = autoMoqer.Create<NotIncludedCodeTracker>();
            Assert.That(notIncludedCodeTracker.Type, Is.EqualTo(ContainingCodeTrackerType.NotIncluded));
        }
    }
    

}
