﻿using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.IndicatorVisibility;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverageTests.Editor.Tagging.Base.Types;
using FineCodeCoverageTests.Editor.Tagging.Types;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using LineSpan = FineCodeCoverageTests.Editor.Tagging.Types.LineSpan;

namespace FineCodeCoverageTests.Editor.Tagging.Base
{
    internal class CoverageTagger_Tests
    {
        [Test]
        public void Should_Listen_For_Changes()
        {
            var autoMoqer = new AutoMoqer();
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(coverageTagger, null));
        }

        [Test]
        public void Should_Unlisten_For_Changes_On_Dispose()
        {
            var autoMoqer = new AutoMoqer();
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            coverageTagger.Dispose();
            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.RemoveListener(coverageTagger));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Raise_Tags_Changed_For_CurrentSnapshot_Range_When_CoverageChangedMessage_Applies_No_Line_Numbers(bool appliesTo)
        {
            var autoMoqer = new AutoMoqer();
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(10);
            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot).Returns(mockTextSnapshot.Object);

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            SnapshotSpan? snapshotSpan = null;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                snapshotSpan = args.Span;
            };
            coverageTagger.Handle(new CoverageChangedMessage(null, appliesTo ? "filepath" : "otherfile", null));

            if (appliesTo)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(snapshotSpan.Value.Snapshot, Is.SameAs(mockTextSnapshot.Object));
                    Assert.That(snapshotSpan.Value.Start.Position, Is.EqualTo(0));
                    Assert.That(snapshotSpan.Value.End.Position, Is.EqualTo(10));
                });
            }
            else
            {
                Assert.That(snapshotSpan, Is.Null);
            }
        }

        [Test]
        public void Should_Raise_Tags_Changed_For_Containing_Range_When_CoverageChangedMessage_With_Line_Numbers()
        {
            var autoMoqer = new AutoMoqer();
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(1000);
            ITextSnapshotLine CreateTextSnapshotLine(int start, int length)
            {
                var mockTextSnapshotLine = new Mock<ITextSnapshotLine>();
                mockTextSnapshotLine.SetupGet(textSnapshotLine => textSnapshotLine.Extent)
                    .Returns(new SnapshotSpan(mockTextSnapshot.Object, start, length));
                return mockTextSnapshotLine.Object;
            };
            mockTextSnapshot.Setup(currentSnapshot => currentSnapshot.GetLineFromLineNumber(1))
                .Returns(CreateTextSnapshotLine(10,10));
            mockTextSnapshot.Setup(currentSnapshot => currentSnapshot.GetLineFromLineNumber(2))
                .Returns(CreateTextSnapshotLine(5,2));
            mockTextSnapshot.Setup(currentSnapshot => currentSnapshot.GetLineFromLineNumber(3))
                .Returns(CreateTextSnapshotLine(15, 10));
            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot).Returns(mockTextSnapshot.Object);
            

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            SnapshotSpan? snapshotSpan = null;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                snapshotSpan = args.Span;
            };
            coverageTagger.Handle(new CoverageChangedMessage(null, "filepath", new List<int> { 1, 2, 3}));


            Assert.Multiple(() =>
            {
                Assert.That(snapshotSpan.Value.Snapshot, Is.SameAs(mockTextSnapshot.Object));
                Assert.That(snapshotSpan.Value.Start.Position, Is.EqualTo(5));
                Assert.That(snapshotSpan.Value.End.Position, Is.EqualTo(25));
            });

        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_HasCoverage_When_Has(bool hasCoverage)
        {
            var coverageTagger = new CoverageTagger<DummyTag>(
                new Mock<ITextInfo>().Object,
                hasCoverage ? new Mock<IBufferLineCoverage>().Object : null,
                new Mock<ICoverageTypeFilter>().Object,
                new Mock<IEventAggregator>().Object,
                new Mock<ILineSpanLogic>(MockBehavior.Strict).Object,
                new Mock<ILineSpanTagger<DummyTag>>().Object,
                new Mock<IFileIndicatorVisibility>().Object
            );

            Assert.That(coverageTagger.HasCoverage, Is.EqualTo(hasCoverage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Raise_TagsChanged_For_CoverageTypeFilterChangedMessage_With_The_Same_TypeIdentifier_If_Has_Coverage(bool same)
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<ICoverageTypeFilter>(new DummyCoverageTypeFilter());
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();

            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot.Length).Returns(10);

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            var tagsChanged = false;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                tagsChanged = true;
            };

            var filter = same ? new DummyCoverageTypeFilter() as ICoverageTypeFilter : new OtherCoverageTypeFilter();
            var coverageTypeFilterChangedMessage = new CoverageTypeFilterChangedMessage(filter);
            coverageTagger.Handle(coverageTypeFilterChangedMessage);

            Assert.That(tagsChanged, Is.EqualTo(same));
        }

        [Test]
        public void Should_Not_Raise_TagsChanged_For_CoverageTypeFilterChangedMessage_If_No_Coverage()
        {
            var coverageTagger = new CoverageTagger<DummyTag>(
                new Mock<ITextInfo>().Object,
                null,
                new DummyCoverageTypeFilter(),
                new Mock<IEventAggregator>().Object,
                new Mock<ILineSpanLogic>(MockBehavior.Strict).Object,
                new Mock<ILineSpanTagger<DummyTag>>().Object,
                new Mock<IFileIndicatorVisibility>().Object
            );

            var tagsChanged = false;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                tagsChanged = true;
            };

            var coverageTypeFilterChangedMessage = new CoverageTypeFilterChangedMessage(new DummyCoverageTypeFilter());
            coverageTagger.Handle(coverageTypeFilterChangedMessage);

            Assert.That(tagsChanged, Is.False);
        }

        [Test]
        public void Should_Return_No_Tags_If_No_Coverage_Lines()
        {
            var coverageTagger = new CoverageTagger<DummyTag>(
                new Mock<ITextInfo>().Object,
                null,
                new Mock<ICoverageTypeFilter>().Object,
                new Mock<IEventAggregator>().Object,
                new Mock<ILineSpanLogic>(MockBehavior.Strict).Object,
                new Mock<ILineSpanTagger<DummyTag>>().Object,
                new Mock<IFileIndicatorVisibility>().Object
            );

            var tags = coverageTagger.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.Empty);
        }

        [Test]
        public void Should_Return_No_Tags_If_ICoverageTypeFilter_Is_Disabled()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<ICoverageTypeFilter>(new DummyCoverageTypeFilter { Disabled = true });
            autoMoqer.SetInstance(new Mock<ILineSpanLogic>(MockBehavior.Strict).Object);

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            var tags = coverageTagger.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_GetLineSpans_From_LineSpanLogic_For_The_Spans_When_Coverage_And_Coverage_Filter_Enabled(bool newCoverage)
        {
            var autoMoqer = new AutoMoqer();
            var bufferLineCoverage = autoMoqer.GetMock<IBufferLineCoverage>().Object;

            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(10);
            mockTextInfo.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot).Returns(mockTextSnapshot.Object);
            mockTextInfo.SetupGet(textBufferWithFilePath => textBufferWithFilePath.FilePath).Returns("filepath");
            autoMoqer.Setup<IFileIndicatorVisibility,bool>(fileIndicatorVisibility => fileIndicatorVisibility.IsVisible(It.IsAny<string>())).Returns(true);
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            var spans = new NormalizedSnapshotSpanCollection();

            var expectedBufferLineCoverageForLogic = bufferLineCoverage;
            if (newCoverage)
            {
                expectedBufferLineCoverageForLogic = new Mock<IBufferLineCoverage>().Object;
                coverageTagger.Handle(new CoverageChangedMessage(expectedBufferLineCoverageForLogic, "filepath",null));
            }

            coverageTagger.GetTags(spans);

            autoMoqer.Verify<ILineSpanLogic>(lineSpanLogic => lineSpanLogic.GetLineSpans(
               expectedBufferLineCoverageForLogic, spans));
        }

        [Test]
        public void Should_GetTagsSpans_For_Filtered_LineSpans()
        {
            var autoMoqer = new AutoMoqer();
            var mockCoverageTypeFilter = autoMoqer.GetMock<ICoverageTypeFilter>();

            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.Covered)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.Partial)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.NotCovered)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.Dirty)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.NewLine)).Returns(true);

            var lineSpans = new List<ILineSpan>
            {
                new LineSpan{  Line = CreateLine(DynamicCoverageType.Covered),Span = SnapshotSpanFactory.Create(1)},
                new LineSpan{  Line = CreateLine(DynamicCoverageType.NotCovered), Span = SnapshotSpanFactory.Create(2)},
                new LineSpan{  Line = CreateLine(DynamicCoverageType.Partial), Span = SnapshotSpanFactory.Create(3)},
                new LineSpan{  Line = CreateLine(DynamicCoverageType.Dirty), Span = SnapshotSpanFactory.Create(4)},
                new LineSpan{  Line = CreateLine(DynamicCoverageType.NewLine), Span = SnapshotSpanFactory.Create(5)},
            };
            var expectedLineSpan = lineSpans[4];

            var mockLineSpanTagger = autoMoqer.GetMock<ILineSpanTagger<DummyTag>>();
            var tagSpan = new TagSpan<DummyTag>(expectedLineSpan.Span, new DummyTag());
            mockLineSpanTagger.Setup(lineSpanTagger => lineSpanTagger.GetTagSpan(expectedLineSpan)).Returns(tagSpan);

            autoMoqer.Setup<ILineSpanLogic, IEnumerable<ILineSpan>>(
                lineSpanLogic => lineSpanLogic.GetLineSpans(
                    It.IsAny<IBufferLineCoverage>(),
                    It.IsAny<NormalizedSnapshotSpanCollection>()
                    )
                )
                .Returns(lineSpans);

            autoMoqer.Setup<ITextInfo,string>(textInfo => textInfo.FilePath).Returns("filepath");
            autoMoqer.Setup<IFileIndicatorVisibility, bool>(fileIndicatorVisibility => fileIndicatorVisibility.IsVisible("filepath")).Returns(true);
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            var tags = coverageTagger.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.EqualTo(new[] { tagSpan }));
            mockCoverageTypeFilter.VerifyAll();

            IDynamicLine CreateLine(DynamicCoverageType coverageType)
            {
                var mockLine = new Mock<IDynamicLine>();
                mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
                return mockLine.Object;
            }
           
        }

        [TestCase(true,false)]
        [TestCase(false, true)]
        public void Should_Raise_TagsChanged_When_FileIndicatorVisibility_Toggled(bool newVisibility,bool expectedTagsChanged)
        {
            var autoMoqer = new AutoMoqer();
            var mockTextBuffer = new Mock<ITextBuffer2>();
            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(10);
            mockTextBuffer.SetupGet(textBuffer => textBuffer.CurrentSnapshot).Returns(mockSnapshot.Object);
            autoMoqer.Setup<ITextInfo,string>(textInfo => textInfo.FilePath).Returns("filepath");
            autoMoqer.Setup<ITextInfo,ITextBuffer>(textInfo => textInfo.TextBuffer).Returns(mockTextBuffer.Object);
            var mockFileIndicatorVisibility = autoMoqer.GetMock<IFileIndicatorVisibility>();
            mockFileIndicatorVisibility.SetupSequence(fileIndicatorVisibility => fileIndicatorVisibility.IsVisible("filepath"))
                .Returns(true)
                .Returns(newVisibility);
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            var tagsChanged = false;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                tagsChanged = true;
            };
            mockFileIndicatorVisibility.Raise(fileIndicatorVisibility => fileIndicatorVisibility.VisibilityChanged += null, EventArgs.Empty);

            Assert.That(tagsChanged, Is.EqualTo(expectedTagsChanged));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Have_No_Tags_When_FileIndicatorVisibility_Is_Initially_False(bool initiallyVisible)
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<ICoverageTypeFilter, bool>(coverageTypeFilter => coverageTypeFilter.Show(DynamicCoverageType.Covered)).Returns(true);
            var mockLineSpan = new Mock<ILineSpan>();
            mockLineSpan.SetupGet(lineSpan => lineSpan.Line.CoverageType).Returns(DynamicCoverageType.Covered);
            autoMoqer.Setup<ILineSpanLogic, IEnumerable<ILineSpan>>(lineSpanLogic =>
                lineSpanLogic.GetLineSpans(It.IsAny<IBufferLineCoverage>(), It.IsAny<NormalizedSnapshotSpanCollection>())
            ).Returns(new List<ILineSpan> { mockLineSpan.Object });

            var mockFileIndicatorVisibility = autoMoqer.GetMock<IFileIndicatorVisibility>();
            mockFileIndicatorVisibility.Setup(fileIndicatorVisibility => fileIndicatorVisibility.IsVisible(It.IsAny<string>()))
                .Returns(initiallyVisible);
            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            var tags = coverageTagger.GetTags(new NormalizedSnapshotSpanCollection()).ToList();

            Assert.That(tags.Count, Is.EqualTo(initiallyVisible ? 1 : 0));
        }

        [Test]
        public void Should_Remove_FileIndicatorVisibility_VisibilityChange_Handler_When_Dispose()
        {
            var autoMoqer = new AutoMoqer();
            var mockFileIndicatorVisibility = autoMoqer.GetMock<IFileIndicatorVisibility>();

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            coverageTagger.Dispose();

            mockFileIndicatorVisibility.VerifyRemove(fileIndicatorVisibility => fileIndicatorVisibility.VisibilityChanged -= It.IsAny<EventHandler>());
        }
    }
}