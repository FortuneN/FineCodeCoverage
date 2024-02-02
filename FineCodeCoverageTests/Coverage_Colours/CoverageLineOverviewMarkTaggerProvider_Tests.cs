using AutoMoq;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    public class CoverageLineOverviewMarkTaggerProvider_Tests
    {
        [Test]
        public void Should_Create_Tagger_From_The_ICoverageTaggerProviderFactory()
        {
            var mocker = new AutoMoqer();

            var textBuffer = new Mock<ITextBuffer>().Object;

            var coverageTagger = new Mock<ICoverageTagger<OverviewMarkTag>>().Object;
            var mockCoverageTaggerProvider = new Mock<ICoverageTaggerProvider<OverviewMarkTag>>();
            mockCoverageTaggerProvider.Setup(coverageTaggerProvider => coverageTaggerProvider.CreateTagger(textBuffer)).Returns(coverageTagger);

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            mockCoverageTaggerProviderFactory.Setup(
                coverageTaggerProviderFactory => coverageTaggerProviderFactory.Create<OverviewMarkTag, CoverageOverviewMarginFilter>(
                    It.IsAny<ILineSpanTagger<OverviewMarkTag>>())
                )
                .Returns(mockCoverageTaggerProvider.Object);

            var coverageLineOverviewMarkTaggerProvider = mocker.Create<CoverageLineOverviewMarkTaggerProvider>();

            var tagger = coverageLineOverviewMarkTaggerProvider.CreateTagger<OverviewMarkTag>(textBuffer);

            Assert.That(tagger, Is.SameAs(coverageTagger));
        }

        [TestCase(CoverageType.Covered)]
        [TestCase(CoverageType.NotCovered)]
        [TestCase(CoverageType.Partial)]
        public void Should_Create_An_OverviewMarkTag_TagSpan_MarkKindName_From_CoverageColoursEditorFormatMapNames_For_The_Line_Coverage_Type(CoverageType coverageType)
        {
            var mocker = new AutoMoqer();
            mocker.Setup<ICoverageColoursEditorFormatMapNames, string>(
                coverageColoursEditorFormatMapNames => coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(coverageType)).Returns("MarkKindName");

            var coverageLineOverviewMarkTaggerProvider = mocker.Create<CoverageLineOverviewMarkTaggerProvider>();

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            var overviewMarkLineSpanTagger = mockCoverageTaggerProviderFactory.Invocations[0].Arguments[0] as ILineSpanTagger<OverviewMarkTag>;

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(1);
            var snapshotSpan = new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 1));
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
            var tagSpan = overviewMarkLineSpanTagger.GetTagSpan(new LineSpan { Line = mockLine.Object, Span = snapshotSpan });

            Assert.Multiple(() =>
            {
                Assert.That(tagSpan.Span, Is.EqualTo(snapshotSpan));
                Assert.That(tagSpan.Tag.MarkKindName, Is.EqualTo("MarkKindName"));
            });
        }
    }
}