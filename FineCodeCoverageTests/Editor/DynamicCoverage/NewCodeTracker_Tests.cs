using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class NewCodeTracker_Tests
    {
        [Test]
        public void Should_Have_No_Lines_Initially()
        {
            var newCodeTracker = new NewCodeTracker(true);

            Assert.That(newCodeTracker.Lines, Is.Empty);
        }

        [TestCase(true)]
        public void Should_Have_A_New_Line_For_All_New_Code_Based_Upon_Language(bool isCSharp)
        {

        }
    }
}
