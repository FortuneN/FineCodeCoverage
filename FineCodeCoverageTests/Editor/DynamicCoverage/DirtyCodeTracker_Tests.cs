using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DirtyCodeTracker_Tests
    {
        [Test]
        public void Should_Be_A_TrackingLineTracker()
        {
            var dirtyCodeTracker = new DirtyCodeTracker(null);
            Assert.That(dirtyCodeTracker, Is.InstanceOf<TrackingLineTracker>());
        }

        [Test]
        public void Should_Have_Correct_ContainingCodeTrackerType()
        {
            var dirtyCodeTracker = new DirtyCodeTracker(null);
            Assert.That(dirtyCodeTracker.Type, Is.EqualTo(ContainingCodeTrackerType.CoverageLines));
        }
    }
    

}
