using AutoMoq;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    public class CoverageLineGlyphTaggerProvider_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_A_CoverageLineGlyphTagger_Using_The_Tagger_From_The_ICoverageTaggerProviderFactory_If_Not_Null(bool isNull)
        {
            var mocker = new AutoMoqer();

            var textBuffer = new Mock<ITextBuffer>().Object;

            var coverageTagger = new Mock<ICoverageTagger<CoverageLineGlyphTag>>().Object;
            var mockCoverageTaggerProvider = new Mock<ICoverageTaggerProvider<CoverageLineGlyphTag>>();
            var createTaggerSetup = mockCoverageTaggerProvider.Setup(coverageTaggerProvider => coverageTaggerProvider.CreateTagger(textBuffer));
            if (!isNull)
            {
                createTaggerSetup.Returns(coverageTagger);
            }

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            mockCoverageTaggerProviderFactory.Setup(
                coverageTaggerProviderFactory => coverageTaggerProviderFactory.Create<CoverageLineGlyphTag, GlyphTagFilter>(
                    It.IsAny<ILineSpanTagger<CoverageLineGlyphTag>>())
                )
                .Returns(mockCoverageTaggerProvider.Object);

            var coverageLineGlyphTaggerProvider = mocker.Create<CoverageLineGlyphTaggerProvider>();

            var tagger = coverageLineGlyphTaggerProvider.CreateTagger<CoverageLineGlyphTag>(textBuffer);
            if (isNull)
            {
                Assert.That(tagger, Is.Null);
            }
            else
            {
                Assert.That(tagger, Is.InstanceOf<CoverageLineGlyphTagger>());
            }

        }

        [TestCase(CoverageType.Covered)]
        [TestCase(CoverageType.NotCovered)]
        [TestCase(CoverageType.Partial)]
        public void Should_Create_A_CoverageLineGlyphTag_TagSpan_BackgroundColor_From_ICoverageColoursProvider_For_The_Line_Coverage_Type_And_The_Line(CoverageType coverageType)
        {
            var mocker = new AutoMoqer();
            var mockCoverageColours = new Mock<ICoverageColours>();
            var mockItemCoverageColours = new Mock<IItemCoverageColours>();
            mockItemCoverageColours.SetupGet(itemCoverageColours => itemCoverageColours.Background).Returns(Colors.Red);
            mockCoverageColours.Setup(coverageColours => coverageColours.GetColour(coverageType)).Returns(mockItemCoverageColours.Object);
            mocker.Setup<ICoverageColoursProvider, ICoverageColours>(
                coverageColoursProvider => coverageColoursProvider.GetCoverageColours()).Returns(mockCoverageColours.Object);

            var coverageLineGlyphTaggerProvider = mocker.Create<CoverageLineGlyphTaggerProvider>();

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            var classificationLineSpanTagger = mockCoverageTaggerProviderFactory.Invocations[0].Arguments[0] as ILineSpanTagger<CoverageLineGlyphTag>;

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(1);
            var snapshotSpan = new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 1));
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
            var tagSpan = classificationLineSpanTagger.GetTagSpan(new LineSpan { Line = mockLine.Object, Span = snapshotSpan });

            Assert.Multiple(() =>
            {
                Assert.That(tagSpan.Span, Is.EqualTo(snapshotSpan));
                Assert.That(tagSpan.Tag.CoverageLine, Is.SameAs(mockLine.Object));
                Assert.That(tagSpan.Tag.Colour, Is.EqualTo(Colors.Red));
            });
        }
    }
}