using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CPPCoverageContentType_Tests
    {
        [Test]
        public void Should_Have_Null_LineExcluder()
        {
            Assert.That(new CPPCoverageContentType().LineExcluder, Is.Null);
        }

        [Test]
        public void Should_Have_Null_FileCodeSpanRangeService()
        {
            Assert.That(new CPPCoverageContentType().FileCodeSpanRangeService, Is.Null);
        }

        [Test]
        public void Should_Have_CPP_ContentType()
        {
            Assert.That(new CPPCoverageContentType().ContentTypeName, Is.EqualTo("C/C++"));
        }
    }
}
