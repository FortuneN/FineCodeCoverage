using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class VBCoverageContentType_Tests
    {
        [Test]
        public void Should_Have_Basic_Content_Type()
        {
            Assert.That(new VBCoverageContentType(null).ContentTypeName, Is.EqualTo("Basic"));
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

            var vbCoverageContentType = new VBCoverageContentType(mockRoslynFileCodeSpanRangeService.Object);

            Assert.That(vbCoverageContentType.FileCodeSpanRangeService, Is.SameAs(fileCodeSpanRangeServiceFromRoslyn));
            Assert.That(vbCoverageContentType.UseFileCodeSpanRangeServiceForChanges, Is.EqualTo(roslynUseFileCodeSpanRangeServiceForChanges));
        }

        [Test]
        public void Should_Allow_For_ILine_Missed_By_Roslyn()
        {
            Assert.False(new VBCoverageContentType(null).CoverageOnlyFromFileCodeSpanRangeService);
        }

        [Test]
        public void Should_LineExclude_Comments()
        {
            Assert.True(new VBCoverageContentType(null).LineExcluder.ExcludeIfNotCode("'"));
        }

        [Test]
        public void Should_LineExclude_REM()
        {
            Assert.True(new VBCoverageContentType(null).LineExcluder.ExcludeIfNotCode("REM"));
        }

        [Test]
        public void Should_LineExclude_Compiler_Directives()
        {
            Assert.True(new VBCoverageContentType(null).LineExcluder.ExcludeIfNotCode("#"));
        }

    }
}
