using AutoMoq;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Editor.Tagging.Classification;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.Editor.Tagging.Types;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;
using System;
using LineSpan = FineCodeCoverageTests.Editor.Tagging.Types.LineSpan;

namespace FineCodeCoverageTests.Editor.Tagging.Classification
{
    internal class CoverageLineClassificationTaggerProvider_Tests
    {
        [Test]
        public void Should_Create_Tagger_From_The_ICoverageTaggerProviderFactory()
        {
            var mocker = new AutoMoqer();

            var textBuffer = new Mock<ITextBuffer>().Object;
            var textView = new Mock<ITextView>().Object;

            var coverageTagger = new Mock<ICoverageTagger<IClassificationTag>>().Object;
            var mockCoverageTaggerProvider = new Mock<ICoverageTaggerProvider<IClassificationTag>>();
            mockCoverageTaggerProvider.Setup(coverageTaggerProvider => coverageTaggerProvider.CreateTagger(textView, textBuffer)).Returns(coverageTagger);

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            mockCoverageTaggerProviderFactory.Setup(
                coverageTaggerProviderFactory => coverageTaggerProviderFactory.Create<IClassificationTag, CoverageClassificationFilter>(
                    It.IsAny<ILineSpanTagger<IClassificationTag>>())
                )
                .Returns(mockCoverageTaggerProvider.Object);

            var coverageLineClassificationTaggerProvider = mocker.Create<CoverageLineClassificationTaggerProvider>();

            var tagger = coverageLineClassificationTaggerProvider.CreateTagger<IClassificationTag>(textView, textBuffer);

            Assert.That(tagger, Is.SameAs(coverageTagger));
        }

        [TestCase(CoverageType.Covered)]
        [TestCase(CoverageType.NotCovered)]
        [TestCase(CoverageType.Partial)]
        public void Should_Create_An_IClassificationTag_TagSpan_Classification_Type_From_ICoverageTypeService_For_The_Line_Coverage_Type(CoverageType coverageType)
        {
            //var mocker = new AutoMoqer();
            //var classificationType = new Mock<IClassificationType>().Object;
            //mocker.Setup<ICoverageTypeService, IClassificationType>(
            //    coverageTypeService => coverageTypeService.GetClassificationType(coverageType)).Returns(classificationType);

            //var coverageLineClassificationTaggerProvider = mocker.Create<CoverageLineClassificationTaggerProvider>();

            //var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            //var classificationLineSpanTagger = mockCoverageTaggerProviderFactory.Invocations[0].Arguments[0] as ILineSpanTagger<IClassificationTag>;

            //var snapshotSpan = SnapshotSpanFactory.Create(1);
            //var mockLine = new Mock<IDynamicLine>();
            //mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
            //var tagSpan = classificationLineSpanTagger.GetTagSpan(new LineSpan { Line = mockLine.Object, Span = snapshotSpan });

            //Assert.Multiple(() =>
            //{
            //    Assert.That(tagSpan.Span, Is.EqualTo(snapshotSpan));
            //    Assert.That(tagSpan.Tag.ClassificationType, Is.SameAs(classificationType));
            //});
            throw new System.NotImplementedException();
        }
    }
}