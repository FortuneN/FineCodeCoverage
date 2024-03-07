using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class LineExcluder_Tests
    {
        [TestCase(true, "   ", true)]
        [TestCase(true, "   //", true)]
        [TestCase(true, "   #pragma", true)]
        [TestCase(true, "   using", true)]
        [TestCase(true, "   not excluded", false)]
        [TestCase(false, "   ", true)]
        [TestCase(false, "   '", true)]
        [TestCase(false, "  REM", true)]
        [TestCase(false, "   #pragma", true)]
        [TestCase(true, "   not excluded", false)]
        public void Should_Exclude_If_Not_Code(bool isCSharp, string text, bool expectedExclude)
        {
            var codeLineExcluder = new LineExcluder();
            var exclude = codeLineExcluder.ExcludeIfNotCode(text, isCSharp);
            Assert.That(exclude, Is.EqualTo(expectedExclude));
        }
    }
}
