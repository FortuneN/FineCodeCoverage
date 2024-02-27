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
        public void Should_Never_Change()
        {
            var otherLinesTracker = new OtherLinesTracker();

            Assert.That(otherLinesTracker.Update(null, null, null), Is.False);
        }
    }
    

}
