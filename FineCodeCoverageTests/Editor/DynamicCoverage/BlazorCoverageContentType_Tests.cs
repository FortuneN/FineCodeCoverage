using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor;
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
            Assert.That(new BlazorCoverageContentType(null).Exclude(filePath), Is.EqualTo(expectedExclude));
        }

        [Test]
        public void Should_Line_Exclude_HtmlTags()
        {
            var lineExcluder = new BlazorCoverageContentType(null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("<"));
        }

        [Test]
        public void Should_Line_Exclude_Directives()
        {
            var lineExcluder = new BlazorCoverageContentType(null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("@"));
        }

        [Test]
        public void Should_Line_Exclude_Comments()
        {
            var lineExcluder = new BlazorCoverageContentType(null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("//"));
        }

        [Test]
        public void Should_Line_Exclude_Compiler_Directives()
        {
            var lineExcluder = new BlazorCoverageContentType(null).LineExcluder;
            Assert.True(lineExcluder.ExcludeIfNotCode("#"));
        }

        [Test]
        public void Should_Not_UseFileCodeSpanRangeServiceForChanges()
        {
            Assert.False(new BlazorCoverageContentType(null).UseFileCodeSpanRangeServiceForChanges);
        }

        [Test]
        public void Should_CoverageFromFileCodeSpanRangeService_And_Additional_Lines()
        {
            Assert.False(new BlazorCoverageContentType(null).CoverageOnlyFromFileCodeSpanRangeService);
        }

        [Test]
        public void Should_Use_BlazorFileCodeSpanRangeService()
        {
            var blazorFileCodeSpanRangeService = new Mock<IBlazorFileCodeSpanRangeService>().Object;
            Assert.That(blazorFileCodeSpanRangeService, Is.SameAs(new BlazorCoverageContentType(blazorFileCodeSpanRangeService).FileCodeSpanRangeService));
        }

        [Test]
        public void Should_Be_For_The_Razor_ContentType()
        {
            Assert.That("Razor", Is.EqualTo(new BlazorCoverageContentType(null).ContentTypeName));
        }

    }
}
