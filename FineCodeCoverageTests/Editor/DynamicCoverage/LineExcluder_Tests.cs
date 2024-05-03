using FineCodeCoverage.Editor.DynamicCoverage;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class LineExcluder_Tests
    {
        [TestCase(new string[0],"   ", true)]
        [TestCase(new string[0], "   x", false)]
        [TestCase(new string[] { "y", "x"}, "   x", true)]
        public void Should_Exclude_If_Not_Code(string[] exclusions, string text, bool expectedExclude)
        {
            var codeLineExcluder = new LineExcluder(exclusions);
            var exclude = codeLineExcluder.ExcludeIfNotCode(text);
            Assert.That(exclude, Is.EqualTo(expectedExclude));
        }
    }
}
