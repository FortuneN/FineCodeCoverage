using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class CSharpCoverageContentType_Tests
    {
        [Test]
        public void Should_Have_CSharp_Content_Type()
        {
            Assert.That(new CSharpCoverageContentType(null).ContentTypeName, Is.EqualTo("CSharp"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Delegate_To_IRoslynFileCodeSpanRangeService(bool roslynUseFileCodeSpanRangeServiceForChanges)
        {
            var fileCodeSpanRangeServiceFromRoslyn = new Mock<IFileCodeSpanRangeService>().Object;
            var mockRoslynFileCodeSpanRangeService = new Mock<IRoslynFileCodeSpanRangeService>();
            mockRoslynFileCodeSpanRangeService.SetupGet(roslynFileCodeSpanRangeService => roslynFileCodeSpanRangeService.FileCodeSpanRangeService)
                .Returns(fileCodeSpanRangeServiceFromRoslyn);

            mockRoslynFileCodeSpanRangeService.SetupGet(roslynFileCodeSpanRangeService => roslynFileCodeSpanRangeService.UseFileCodeSpanRangeServiceForChanges)
                .Returns(roslynUseFileCodeSpanRangeServiceForChanges);

            var cSharpCoverageContentType = new CSharpCoverageContentType(mockRoslynFileCodeSpanRangeService.Object);

            Assert.That(cSharpCoverageContentType.FileCodeSpanRangeService, Is.SameAs(fileCodeSpanRangeServiceFromRoslyn));
            Assert.That(cSharpCoverageContentType.UseFileCodeSpanRangeServiceForChanges, Is.EqualTo(roslynUseFileCodeSpanRangeServiceForChanges));
        }

        [Test]
        public void Should_Allow_For_ILine_Missed_By_Roslyn()
        {
            Assert.False(new CSharpCoverageContentType(null).CoverageOnlyFromFileCodeSpanRangeService);
        }

        [Test]
        public void Should_LineExclude_Comments()
        {
            Assert.True(new CSharpCoverageContentType(null).LineExcluder.ExcludeIfNotCode("//"));
        }

        [Test]
        public void Should_LineExclude_Usings()
        {
            Assert.True(new CSharpCoverageContentType(null).LineExcluder.ExcludeIfNotCode("using"));
        }

        [Test]
        public void Should_LineExclude_Compiler_Directives()
        {
            Assert.True(new CSharpCoverageContentType(null).LineExcluder.ExcludeIfNotCode("#"));
        }

    }
}
