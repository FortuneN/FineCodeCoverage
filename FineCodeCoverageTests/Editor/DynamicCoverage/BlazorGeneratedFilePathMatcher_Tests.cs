using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class BlazorGeneratedFilePathMatcher_Tests
    {
        [TestCase("razorpath","razorpath.",true)]
        [TestCase("razorpath", "razorpathx.", false)]
        public void Should_Be_Generated_If_File_Path_Starts_With_Razor_Path_And_Dot(
            string razorFilePath,
            string generatedFilePath,
            bool expectedIsGenerated
        )
        {
            var isGenerated = new BlazorGeneratedFilePathMatcher().IsBlazorGeneratedFilePath(razorFilePath, generatedFilePath);

            Assert.That(expectedIsGenerated, Is.EqualTo(isGenerated));
        }
    }
}
