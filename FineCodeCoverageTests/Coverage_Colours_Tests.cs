using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests
{
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

        public Func<DummyCoverageTypeFilter,bool> ChangedFunc { get; set; }
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

    internal class DummyTag : ITag { }

    internal class DummyTagger : IListener<NewCoverageLinesMessage>, IListener<CoverageTypeFilterChangedMessage>, ITagger<DummyTag>,IDisposable
    {
        

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITagSpan<DummyTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            throw new NotImplementedException();
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            throw new NotImplementedException();
        }

        public void Handle(CoverageTypeFilterChangedMessage message)
        {
            throw new NotImplementedException();
        }
    }
    
    public class CoverageLineTaggerProviderBase_Tests
    {
        [Test]
        public void Should_Add_Itself_As_An_Event_Listener_For_NewCoverageLinesMessage()
        {
            var mockEventAggregator = new Mock<IEventAggregator>();
            var coverageLineTaggerProviderBase = new Mock<CoverageLineTaggerProviderBase<DummyTagger, DummyTag, DummyCoverageTypeFilter>>(
                mockEventAggregator.Object,
                new Mock<IAppOptionsProvider>().Object,
                new Mock<ILineSpanLogic>().Object
            ).Object;

            mockEventAggregator.Verify(eventAggregator => eventAggregator.AddListener(coverageLineTaggerProviderBase,null));
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
            var mockEventAggregator = new Mock<IEventAggregator>();
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            mockAppOptionsProvider.Setup(appOptionsProvider => appOptionsProvider.Get()).Returns(firstOptions);
            var coverageLineTaggerProviderBase = new Mock<CoverageLineTaggerProviderBase<DummyTagger, DummyTag, DummyCoverageTypeFilter>>(
                mockEventAggregator.Object,
                mockAppOptionsProvider.Object,
                new Mock<ILineSpanLogic>().Object
            ).Object;

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, changedOptions);

           

            mockEventAggregator.Verify(eventAggregator => eventAggregator.SendMessage(
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
            var mockEventAggregator = new Mock<IEventAggregator>();
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var coverageLineTaggerProviderBase = new Mock<CoverageLineTaggerProviderBase<DummyTagger, DummyTag, DummyCoverageTypeFilter>>(
                mockEventAggregator.Object,
                mockAppOptionsProvider.Object,
                new Mock<ILineSpanLogic>().Object
            ).Object;

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
        }

        [Test]
        public void Should_Add_The_Implementation_Created_Tagger_As_An_Event_Listener()
        {
            DummyCoverageTypeFilter lastFilter = null;
            DummyCoverageTypeFilter.Initialized += (sender, args) =>
            {
                lastFilter = args.DummyCoverageTypeFilter;
                lastFilter.ChangedFunc = other => true;
            };

            var mockEventAggregator = new Mock<IEventAggregator>();
            var mockAppOptionsProvider = new Mock<IAppOptionsProvider>();
            var lineSpanLogic = new Mock<ILineSpanLogic>().Object;
            var mockCoverageLineTaggerProviderBase = new Mock<CoverageLineTaggerProviderBase<DummyTagger, DummyTag, DummyCoverageTypeFilter>>(
                mockEventAggregator.Object,
                mockAppOptionsProvider.Object,
                lineSpanLogic
            );

            var textBuffer = new Mock<ITextBuffer>().Object;

            var dummyTagger = new DummyTagger();
            FileLineCoverage fileLineCoverage = new FileLineCoverage();
            mockCoverageLineTaggerProviderBase.Protected().
                Setup<DummyTagger>("CreateCoverageTagger", 
                    textBuffer, 
                    fileLineCoverage, 
                    mockEventAggregator.Object, 
                    ItExpr.Is<DummyCoverageTypeFilter>(coverageTypeFiltter => coverageTypeFiltter == lastFilter),
                    lineSpanLogic
                )
                .Returns(dummyTagger);
            var coverageLineTaggerProviderBase  = mockCoverageLineTaggerProviderBase.Object;

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
            coverageLineTaggerProviderBase.Handle(new NewCoverageLinesMessage { CoverageLines = fileLineCoverage });

            var tagger = coverageLineTaggerProviderBase.CreateTagger<DummyTag>(textBuffer);
            Assert.That(tagger, Is.SameAs(dummyTagger));
            mockEventAggregator.Verify(eventAggregator => eventAggregator.AddListener(dummyTagger, null));
        }
    }

    public class CoverageLineTaggerBase_Tests
    {
        private Mock<ITextBuffer> GetMockTextBuffer(Action<PropertyCollection> setUpPropertyCollection = null)
        {
            var mockTextBuffer = new Mock<ITextBuffer>();
            var propertyCollection = new PropertyCollection();
            mockTextBuffer.SetupGet(textBuffer => textBuffer.Properties).Returns(propertyCollection);
            setUpPropertyCollection?.Invoke(propertyCollection);
            return mockTextBuffer;
        }
        private Mock<ITextBuffer> GetMockTextBufferForFile()
        {
            return GetMockTextBuffer(propertyCollection =>
            {
                var mockDocument = new Mock<ITextDocument>();
                var filePath = "filepath";
                mockDocument.SetupGet(textDocument => textDocument.FilePath).Returns(filePath);
                propertyCollection[typeof(ITextDocument)] = mockDocument.Object;
            });

        }

        [Test]
        public void Should_Return_No_Tags_If_No_Coverage_Lines()
        {
            var coverageLineTaggerBase = new Mock<CoverageLineTaggerBase<DummyTag>>(GetMockTextBufferForFile().Object, null,null,null,null).Object;
            var tags = coverageLineTaggerBase.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.Empty);
        }

        [Test]
        public void Should_Return_No_Tags_If_No_File_Path()
        {
            
            var coverageLineTaggerBase = new Mock<CoverageLineTaggerBase<DummyTag>>(GetMockTextBuffer().Object, null, null, null, null).Object;
            var tags = coverageLineTaggerBase.GetTags(new NormalizedSnapshotSpanCollection());

            Assert.That(tags, Is.Empty);
        }

        [Test]
        public void Should_Return_No_Tags_If_ICoverageTypeFilter_Is_Disabled()
        {

        }
    }
}
