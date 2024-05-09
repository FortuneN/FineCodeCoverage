using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
using FineCodeCoverage.Options;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class BlazorCoverageContentType_Tests
    {
        [TestCase("path.razor",false)]
        [TestCase("path.cshtml", true)]
        [TestCase("path.vbhtml", true)]
        public void Should_Include_Razor_Component_Files(string filePath, bool expectedExclude)
        {
            Assert.That(new BlazorCoverageContentType(null, null).Exclude(filePath), Is.EqualTo(expectedExclude));
        }

        [Test]
        public void Should_Line_Exclude_HtmlTags()
        {
            var lineExcluder = new BlazorCoverageContentType(null, null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("<"));
        }

        [Test]
        public void Should_Line_Exclude_Directives()
        {
            var lineExcluder = new BlazorCoverageContentType(null, null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("@"));
        }

        [Test]
        public void Should_Line_Exclude_Comments()
        {
            var lineExcluder = new BlazorCoverageContentType(null, null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("//"));
        }

        [Test]
        public void Should_Line_Exclude_Compiler_Directives()
        {
            var lineExcluder = new BlazorCoverageContentType(null, null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("#"));
        }

        [Test]
        public void Should_Not_UseFileCodeSpanRangeServiceForChanges()
        {
            Assert.False(new BlazorCoverageContentType(null, null).UseFileCodeSpanRangeServiceForChanges);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_CoverageFromFileCodeSpanRangeService_From_AppOptions(bool blazorCoverageLinesFromGeneratedSource)
        {
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(a => a.Get()).Returns(new AppOptions { BlazorCoverageLinesFromGeneratedSource = blazorCoverageLinesFromGeneratedSource });
            Assert.That(new BlazorCoverageContentType(null, mockAppOptionsProvider.Object).CoverageOnlyFromFileCodeSpanRangeService, Is.EqualTo(blazorCoverageLinesFromGeneratedSource));
        }

        [Test]
        public void Should_Use_BlazorFileCodeSpanRangeService()
        {
            var blazorFileCodeSpanRangeService = new Mock<IBlazorFileCodeSpanRangeService>().Object;
            Assert.That(blazorFileCodeSpanRangeService, Is.SameAs(new BlazorCoverageContentType(blazorFileCodeSpanRangeService, null).FileCodeSpanRangeService));
        }

        [Test]
        public void Should_Be_For_The_Razor_ContentType()
        {
            Assert.That("Razor", Is.EqualTo(new BlazorCoverageContentType(null, null).ContentTypeName));
        }

    }
}
