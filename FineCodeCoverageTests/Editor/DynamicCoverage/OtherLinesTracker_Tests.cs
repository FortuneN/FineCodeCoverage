using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class OtherLinesTracker_Tests
    {
        [Test]
        public void Should_Not_Have_Lines()
        {
            var otherLinesTracker = new OtherLinesTracker();

            Assert.That(otherLinesTracker.Lines, Is.Empty);
        }

        [Test]
        public void Should_Never_Have_Updated_Line_Numbers()
        {
            var otherLinesTracker = new OtherLinesTracker();

            Assert.That(otherLinesTracker.GetUpdatedLineNumbers(null, null, null), Is.Empty);
        }

        [Test]
        public void Should_Have_Correct_ContainingCodeTrackerType()
        {
            var otherLinesTracker = new OtherLinesTracker();

            Assert.That(otherLinesTracker.Type, Is.EqualTo(ContainingCodeTrackerType.OtherLines));
        }
    }
    

}
