using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests
{
    public class CoverageLineGlyphTagger_Tests
    {
        [Test]
        public void Should_Listen_For_CoverageColoursChangedMessage()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(coverageLineGlyphTagger, null));
        }

        [Test]
        public void Should_Unlisten_On_Dispose()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();

            coverageLineGlyphTagger.Dispose();
            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.RemoveListener(coverageLineGlyphTagger));
        }

        [Test]
        public void Should_Dispose_The_CoverageTagger_On_Dispose()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();

            coverageLineGlyphTagger.Dispose();

            autoMoqer.Verify<ICoverageTagger<CoverageLineGlyphTag>>(coverageTagger => coverageTagger.Dispose());
        }

        [Test]
        public void Should_TagsChanged_When_CoverageTagger_TagsChanged()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();
            var coverageTaggerArgs = new SnapshotSpanEventArgs(new SnapshotSpan());
            SnapshotSpanEventArgs raisedArgs = null;
            coverageLineGlyphTagger.TagsChanged += (sender, args) =>
            {
                raisedArgs = args;
            };
            autoMoqer.GetMock<ICoverageTagger<CoverageLineGlyphTag>>()
                .Raise(coverageTagger => coverageTagger.TagsChanged += null, coverageTaggerArgs);

            Assert.That(raisedArgs, Is.SameAs(coverageTaggerArgs));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_RaiseTagsChanged_On_CoverageTagger_When_Receive_CoverageColoursChangedMessage_And_There_Is_Coverage(bool hasCoverage)
        {
            var autoMoqer = new AutoMoqer();
            var mockCoverageTagger = autoMoqer.GetMock<ICoverageTagger<CoverageLineGlyphTag>>();
            mockCoverageTagger.SetupGet(ct => ct.HasCoverage).Returns(hasCoverage);
            
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();

            coverageLineGlyphTagger.Handle(new CoverageColoursChangedMessage());

            autoMoqer.Verify<ICoverageTagger<CoverageLineGlyphTag>>(coverageTagger => coverageTagger.RaiseTagsChanged(),hasCoverage ? Times.Once() : Times.Never());
        }

        [Test]
        public void Should_GetTags_From_The_CoverageTagger()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();
            var spans = new NormalizedSnapshotSpanCollection();
            var expectedTags = new TagSpan<CoverageLineGlyphTag>[0];
            autoMoqer.GetMock<ICoverageTagger<CoverageLineGlyphTag>>()
                .Setup(coverageTagger => coverageTagger.GetTags(spans))
                .Returns(expectedTags);

            var tags = coverageLineGlyphTagger.GetTags(spans);

            Assert.That(tags, Is.SameAs(expectedTags));
        }
    }
}