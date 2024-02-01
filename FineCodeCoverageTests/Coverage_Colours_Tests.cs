using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FineCodeCoverageTests
{
    internal class LineSpan : ILineSpan
    {
        public ILine Line { get; set; }

        public SnapshotSpan Span { get; set; }
    }
    public static class SnapshotSpanFactory
    {
        public static SnapshotSpan Create(int end)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(end + 1);
            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, end));
        }
    }
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
            mocker.Setup<ICoverageColoursEditorFormatMapNames,string>(
                coverageColoursEditorFormatMapNames => coverageColoursEditorFormatMapNames.GetEditorFormatDefinitionName(coverageType)).Returns("MarkKindName");

            var coverageLineOverviewMarkTaggerProvider = mocker.Create<CoverageLineOverviewMarkTaggerProvider>();

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            var overviewMarkLineSpanTagger = mockCoverageTaggerProviderFactory.Invocations[0].Arguments[0] as ILineSpanTagger<OverviewMarkTag>;

            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(1);
            var snapshotSpan = new SnapshotSpan(mockTextSnapshot.Object, new Span(0,1));
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

    public class CoverageLineClassificationTaggerProvider_Tests
    {
        [Test]
        public void Should_Create_Tagger_From_The_ICoverageTaggerProviderFactory()
        {
            var mocker = new AutoMoqer();

            var textBuffer = new Mock<ITextBuffer>().Object;

            var coverageTagger = new Mock<ICoverageTagger<IClassificationTag>>().Object;
            var mockCoverageTaggerProvider = new Mock<ICoverageTaggerProvider<IClassificationTag>>();
            mockCoverageTaggerProvider.Setup(coverageTaggerProvider => coverageTaggerProvider.CreateTagger(textBuffer)).Returns(coverageTagger);

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            mockCoverageTaggerProviderFactory.Setup(
                coverageTaggerProviderFactory => coverageTaggerProviderFactory.Create<IClassificationTag, CoverageClassificationFilter>(
                    It.IsAny<ILineSpanTagger<IClassificationTag>>())
                )
                .Returns(mockCoverageTaggerProvider.Object);

            var coverageLineClassificationTaggerProvider = mocker.Create<CoverageLineClassificationTaggerProvider>();

            var tagger = coverageLineClassificationTaggerProvider.CreateTagger<IClassificationTag>(textBuffer);

            Assert.That(tagger, Is.SameAs(coverageTagger));
        }

        [TestCase(CoverageType.Covered)]
        [TestCase(CoverageType.NotCovered)]
        [TestCase(CoverageType.Partial)]
        public void Should_Create_An_IClassificationTag_TagSpan_Classification_Type_From_ICoverageTypeService_For_The_Line_Coverage_Type(CoverageType coverageType)
        {
            var mocker = new AutoMoqer();
            var classificationType = new Mock<IClassificationType>().Object;
            mocker.Setup<ICoverageTypeService, IClassificationType>(
                coverageTypeService => coverageTypeService.GetClassificationType(coverageType)).Returns(classificationType);

            var coverageLineClassificationTaggerProvider = mocker.Create<CoverageLineClassificationTaggerProvider>();

            var mockCoverageTaggerProviderFactory = mocker.GetMock<ICoverageTaggerProviderFactory>();
            var classificationLineSpanTagger = mockCoverageTaggerProviderFactory.Invocations[0].Arguments[0] as ILineSpanTagger<IClassificationTag>;

            var snapshotSpan = SnapshotSpanFactory.Create(1);
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
            var tagSpan = classificationLineSpanTagger.GetTagSpan(new LineSpan { Line = mockLine.Object, Span = snapshotSpan });

            Assert.Multiple(() =>
            {
                Assert.That(tagSpan.Span, Is.EqualTo(snapshotSpan));
                Assert.That(tagSpan.Tag.ClassificationType, Is.SameAs(classificationType));
            });
        }
    }

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

    internal class DummyCoverageTypeFilterInitializedEventArgs
    {
        public DummyCoverageTypeFilterInitializedEventArgs(DummyCoverageTypeFilter dummyCoverageTypeFilter)
        {
            DummyCoverageTypeFilter = dummyCoverageTypeFilter;
        }

        public DummyCoverageTypeFilter DummyCoverageTypeFilter { get; }
    }
    internal class DummyCoverageTypeFilter : ICoverageTypeFilter
    {
        public static event EventHandler<DummyCoverageTypeFilterInitializedEventArgs> Initialized;

        public bool Disabled { get; set; }

        public string TypeIdentifier => "Dummy";

        public bool Show(CoverageType coverageType)
        {
            throw new NotImplementedException();
        }

        public Func<DummyCoverageTypeFilter, bool> ChangedFunc { get; set; }
        public bool Changed(ICoverageTypeFilter other)
        {
            return ChangedFunc(other as DummyCoverageTypeFilter);
        }
        public IAppOptions AppOptions { get; private set; }
        public void Initialize(IAppOptions appOptions)
        {
            AppOptions = appOptions;
            Initialized?.Invoke(this, new DummyCoverageTypeFilterInitializedEventArgs(this));
        }
    }
    internal class OtherCoverageTypeFilter : ICoverageTypeFilter
    {
        public bool Disabled => throw new NotImplementedException();

        public string TypeIdentifier => "Other";

        public bool Changed(ICoverageTypeFilter other)
        {
            throw new NotImplementedException();
        }

        public void Initialize(IAppOptions appOptions)
        {
            throw new NotImplementedException();
        }

        public bool Show(CoverageType coverageType)
        {
            throw new NotImplementedException();
        }
    }

    internal class DummyTag : ITag { }

    public class CoverageTaggerProvider_Tests
    {
        [Test]
        public void Should_Add_Itself_As_An_Event_Listener_For_NewCoverageLinesMessage()
        {
            var autoMocker = new AutoMoqer();
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            autoMocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(coverageTaggerProvider, null));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Should_Send_CoverageTypeFilterChangedMessage_With_The_New_Filter_When_AppOptions_Change_For_the_Filter(bool filterChanged)
        {
            var firstOptions = new AppOptions();
            var changedOptions = new AppOptions();
            var isFirst = true;
            DummyCoverageTypeFilter firstFilter = null;
            DummyCoverageTypeFilter secondFilter = null;
            DummyCoverageTypeFilter.Initialized += (sender, args) =>
            {
                var filter = args.DummyCoverageTypeFilter;
                if (isFirst)
                {
                    firstFilter = filter;
                    Assert.That(filter.AppOptions, Is.SameAs(firstOptions));
                }
                else
                {
                    secondFilter = filter;
                    secondFilter.ChangedFunc = other =>
                    {
                        Assert.That(firstFilter, Is.SameAs(other));
                        Assert.That(secondFilter.AppOptions, Is.SameAs(changedOptions));
                        return filterChanged;
                    };
                }
                isFirst = false;
            };
            
            var autoMocker = new AutoMoqer();
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(firstOptions);
            
            var coverageLineTaggerProviderBase = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, changedOptions);

            autoMocker.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(
                    It.Is<CoverageTypeFilterChangedMessage>(coverageTypeFilterChangedMessage => coverageTypeFilterChangedMessage.Filter == secondFilter),
                    null
                ), filterChanged ? Times.Once() : Times.Never()
            );
        }

        [Test]
        public void Should_Use_The_Last_Filter_For_Comparisons()
        {
            var filters = new List<DummyCoverageTypeFilter>();
            DummyCoverageTypeFilter.Initialized += (sender, args) =>
            {
                var filter = args.DummyCoverageTypeFilter;
                filter.ChangedFunc = other =>
                {
                    var index = filters.IndexOf(filter);
                    var lastFilter = filters.IndexOf(other);
                    Assert.That(index - lastFilter, Is.EqualTo(1));
                    return true;
                };
                filters.Add(filter);
            };
            
            var autoMocker = new AutoMoqer();
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();
            
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
        }

        private Mock<ITextBuffer> GetMockTextBuffer(Action<PropertyCollection> setUpPropertyCollection = null)
        {
            var mockTextBuffer = new Mock<ITextBuffer>();
            var propertyCollection = new PropertyCollection();
            mockTextBuffer.SetupGet(textBuffer => textBuffer.Properties).Returns(propertyCollection);
            setUpPropertyCollection?.Invoke(propertyCollection);
            return mockTextBuffer;
        }

        private Mock<ITextBuffer> GetMockTextBufferForFile(string filePath)
        {
            return GetMockTextBuffer(propertyCollection =>
            {
                var mockDocument = new Mock<ITextDocument>();
                mockDocument.SetupGet(textDocument => textDocument.FilePath).Returns(filePath);
                propertyCollection[typeof(ITextDocument)] = mockDocument.Object;
            });
        }

        [Test]
        public void Should_Not_Create_A_Coverage_Tagger_When_The_TextBuffer_Has_No_Associated_Document()
        {
            var autoMocker = new AutoMoqer();
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            var tagger = coverageTaggerProvider.CreateTagger(GetMockTextBuffer().Object);

            Assert.That(tagger, Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_A_Coverage_Tagger_With_Last_Coverage_Lines_And_Last_Coverage_Type_Filter_When_The_TextBuffer_Has_An_Associated_Document(bool newCoverageLinesMessage)
        {
            DummyCoverageTypeFilter lastFilter = null;
            DummyCoverageTypeFilter.Initialized += (sender, args) =>
            {
                lastFilter = args.DummyCoverageTypeFilter;
                lastFilter.ChangedFunc = other =>
                {
                    return true;
                };
                
            };
            var autoMocker = new AutoMoqer();
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();
            IFileLineCoverage fileLineCoverage = null;
            if(newCoverageLinesMessage)
            {
                fileLineCoverage = new Mock<IFileLineCoverage>().Object;
                coverageTaggerProvider.Handle(new NewCoverageLinesMessage { CoverageLines = fileLineCoverage });
            }
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());

            var tagger = coverageTaggerProvider.CreateTagger(GetMockTextBufferForFile("filepath").Object);
           
            Assert.That(tagger, Is.InstanceOf<CoverageTagger<DummyTag>>());
            
            var coverageTaggerType = typeof(CoverageTagger<DummyTag>);
            
            var fileLineCoverageArg = coverageTaggerType.GetField("coverageLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as IFileLineCoverage;
            var coverageTypeFilterArg = coverageTaggerType.GetField("coverageTypeFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as ICoverageTypeFilter;
            var filePathArg = coverageTaggerType.GetField("filePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as string;

            Assert.Multiple(() =>
            {
                Assert.That(filePathArg, Is.EqualTo("filepath"));
                Assert.That(fileLineCoverageArg, Is.SameAs(fileLineCoverage));
                Assert.That(coverageTypeFilterArg, Is.SameAs(lastFilter));
            });
        }
    }

    public class CoverageTagger_Tests
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

        [Test]
        public void Should_Raise_Tags_Changed_For_CurrentSnapshot_Range_When_NewCoverageLinesMessage()
        {
            var autoMoqer = new AutoMoqer();
            var mockTextBufferAndFile = autoMoqer.GetMock<ITextBufferWithFilePath>();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(10);
            mockTextBufferAndFile.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot).Returns(mockTextSnapshot.Object);

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            SnapshotSpan? snapshotSpan = null;
            coverageTagger.TagsChanged += (sender, args) =>
            {
                snapshotSpan = args.Span;
            };
            coverageTagger.Handle(new NewCoverageLinesMessage());

            Assert.Multiple(() =>
            {
                Assert.That(snapshotSpan.Value.Snapshot, Is.SameAs(mockTextSnapshot.Object));
                Assert.That(snapshotSpan.Value.Start.Position, Is.EqualTo(0));
                Assert.That(snapshotSpan.Value.End.Position, Is.EqualTo(10));
            });
           
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Raise_TagsChanged_For_CoverageTypeFilterChangedMessage_With_The_Same_TypeIdentifier(bool same)
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.SetInstance<ICoverageTypeFilter>(new DummyCoverageTypeFilter());
            var mockTextBufferAndFile = autoMoqer.GetMock<ITextBufferWithFilePath>();

            mockTextBufferAndFile.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot.Length).Returns(10);

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
        public void Should_Return_No_Tags_If_No_Coverage_Lines()
        {
            var coverageTagger = new CoverageTagger<DummyTag>(
                new Mock<ITextBufferWithFilePath>().Object,
                null,
                new Mock<ICoverageTypeFilter>().Object,
                new Mock<IEventAggregator>().Object,
                new Mock<ILineSpanLogic>(MockBehavior.Strict).Object,
                new Mock<ILineSpanTagger<DummyTag>>().Object
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
        public void Should_GetLineSpans_From_LineSpanLogic_For_The_FilePath_And_Spans_When_Coverage_And_Coverage_Filter_Enabled(bool newCoverage)
        {
            var autoMoqer = new AutoMoqer();
            var fileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>().Object;

            var mockTextBufferAndFile = autoMoqer.GetMock<ITextBufferWithFilePath>();
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(currentSnapshot => currentSnapshot.Length).Returns(10);
            mockTextBufferAndFile.SetupGet(textBufferAndFile => textBufferAndFile.TextBuffer.CurrentSnapshot).Returns(mockTextSnapshot.Object);
            mockTextBufferAndFile.SetupGet(textBufferWithFilePath => textBufferWithFilePath.FilePath).Returns("filepath");

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();
            var spans = new NormalizedSnapshotSpanCollection();

            var expectedFileLineCoverageForLogic = fileLineCoverage;
            if (newCoverage)
            {
                expectedFileLineCoverageForLogic = new Mock<IFileLineCoverage>().Object;
                coverageTagger.Handle(new NewCoverageLinesMessage { CoverageLines = expectedFileLineCoverageForLogic });
            }

            coverageTagger.GetTags(spans);

            autoMoqer.Verify<ILineSpanLogic>(lineSpanLogic => lineSpanLogic.GetLineSpans(
               expectedFileLineCoverageForLogic, "filepath", spans));
        }

        [Test]
        public void Should_GetTagsSpans_For_Filtered_LineSpans()
        {
            var autoMoqer = new AutoMoqer();
            var mockCoverageTypeFilter = autoMoqer.GetMock<ICoverageTypeFilter>();
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(CoverageType.Covered)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(CoverageType.NotCovered)).Returns(false);
            mockCoverageTypeFilter.Setup(coverageTypeFilter => coverageTypeFilter.Show(CoverageType.Partial)).Returns(true);

            var lineSpans = new List<ILineSpan>
            {
                new LineSpan{  Line = CreateLine(CoverageType.Covered),Span = SnapshotSpanFactory.Create(1)},
                new LineSpan{  Line = CreateLine(CoverageType.NotCovered), Span = SnapshotSpanFactory.Create(2)},
                new LineSpan{  Line = CreateLine(CoverageType.Partial), Span = SnapshotSpanFactory.Create(3)},
            };
            var expectedLineSpan = lineSpans[2];

            var mockLineSpanTagger = autoMoqer.GetMock<ILineSpanTagger<DummyTag>>();
            var tagSpan = new TagSpan<DummyTag>(expectedLineSpan.Span, new DummyTag());
            mockLineSpanTagger.Setup(lineSpanTagger => lineSpanTagger.GetTagSpan(expectedLineSpan)).Returns(tagSpan);

            autoMoqer.Setup<ILineSpanLogic,IEnumerable<ILineSpan>>(
                lineSpanLogic => lineSpanLogic.GetLineSpans(
                    It.IsAny<IFileLineCoverage>(), 
                    It.IsAny<string>(), 
                    It.IsAny<NormalizedSnapshotSpanCollection>()
                    )
                )
                .Returns(lineSpans);

            var coverageTagger = autoMoqer.Create<CoverageTagger<DummyTag>>();

            var tags = coverageTagger.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.EqualTo(new[] { tagSpan }));
            mockCoverageTypeFilter.VerifyAll();

            ILine CreateLine(CoverageType coverageType)
            {
                var mockLine = new Mock<ILine>();
                mockLine.SetupGet(line => line.CoverageType).Returns(coverageType);
                return mockLine.Object;
            }
        }
    }

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

        [Test]
        public void Should_RaiseTagsChanged_On_CoverageTagger_When_Receive_CoverageColoursChangedMessage()
        {
            var autoMoqer = new AutoMoqer();
            var coverageLineGlyphTagger = autoMoqer.Create<CoverageLineGlyphTagger>();

            coverageLineGlyphTagger.Handle(new CoverageColoursChangedMessage(null));

            autoMoqer.Verify<ICoverageTagger<CoverageLineGlyphTag>>(coverageTagger => coverageTagger.RaiseTagsChanged());
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