using System.Collections.Generic;
using AutoMoq;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using FineCodeCoverage.Editor.Roslyn;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class RoslynFileCodeSpanRangeService_Tests
    {
        [TestCase(EditorCoverageColouringMode.UseRoslynWhenTextChanges,true)]
        [TestCase(EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges, false)]
        public void Should_UseFileCodeSpanRangeServiceForChanges_When_Options_Not_DoNotUseRoslynWhenTextChanges(
            EditorCoverageColouringMode editorCoverageColouringMode,
            bool expectedUseFileCodeSpanRangeServiceForChanges
        )
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance(new TestThreadHelper());
            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(editorCoverageColouringMode);
            var mockAppOptionsProvider = autoMoqer.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);

            var roslynFileCodeSpanRangeService = autoMoqer.Create<RoslynFileCodeSpanRangeService>();

            Assert.That(roslynFileCodeSpanRangeService.UseFileCodeSpanRangeServiceForChanges, Is.EqualTo(expectedUseFileCodeSpanRangeServiceForChanges));
        }

        [Test]
        public void Should_GetFileCodeSpanRanges_By_Converting_The_Coverage_TextSpans_Using_The_TextSnapshot()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<IThreadHelper>(new TestThreadHelper());

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(0)).Returns(1);
            mockTextSnapshot.Setup(textSnapshot => textSnapshot.GetLineNumberFromPosition(10)).Returns(2);
            var mockRoslynService = autoMoqer.GetMock<IRoslynService>();
            mockRoslynService.Setup(roslynService => roslynService.GetContainingCodeSpansAsync(mockTextSnapshot.Object))
                .ReturnsAsync(new List<TextSpan> { new TextSpan(0, 10) });

            var roslynFileCodeSpanRangeService = autoMoqer.Create<RoslynFileCodeSpanRangeService>();
            var fileCodeSpanRanges = roslynFileCodeSpanRangeService.GetFileCodeSpanRanges(mockTextSnapshot.Object);

            Assert.That(fileCodeSpanRanges, Is.EqualTo(new List<CodeSpanRange> { new CodeSpanRange(1, 2) }));
        }

        [Test]
        public void Should_Return_Itself_As_FileCodeSpanRangeService()
        {
            var autoMoqer = new AutoMoqer();
            var roslynFileCodeSpanRangeService = autoMoqer.Create<RoslynFileCodeSpanRangeService>();

            Assert.That(roslynFileCodeSpanRangeService.FileCodeSpanRangeService, Is.SameAs(roslynFileCodeSpanRangeService));
        }
    }
}
