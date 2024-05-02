using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;
using FineCodeCoverageTests.Editor.Tagging.Base.Types;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.Tagging.Base
{
    internal class CoverageTaggerProvider_Tests
    {
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
            autoMocker.SetInstance(new IFileExcluder[0]);
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
            autoMocker.SetInstance(new IFileExcluder[0]);
            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());
        }

        [Test]
        public void Should_Not_Create_A_Coverage_Tagger_When_The_TextBuffer_Associated_Document_Has_No_FilePath()
        {
            var autoMocker = new AutoMoqer();
            autoMocker.SetInstance(new IFileExcluder[0]);
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns((string)null);
            autoMocker.Setup<ITextInfoFactory,ITextInfo>(textInfoFactory => textInfoFactory.Create(It.IsAny<ITextView>(), It.IsAny<ITextBuffer>()))
                .Returns(mockTextInfo.Object);
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            var tagger = coverageTaggerProvider.CreateTagger(new Mock<ITextView>().Object, new Mock<ITextBuffer>().Object);

            Assert.That(tagger, Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Not_Create_A_Coverage_Tagger_When_FilePath_Is_Excluded(bool isExcluded)
        {
            var autoMocker = new AutoMoqer();
            var filePath = "filePath";
            var firstFileExcluder = new Mock<IFileExcluder>().Object;
            var mockSecondFileExcluder = new Mock<IFileExcluder>();
            mockSecondFileExcluder.Setup(fileExcluder => fileExcluder.Exclude(filePath)).Returns(isExcluded);
            autoMocker.SetInstance(new IFileExcluder[] { firstFileExcluder, mockSecondFileExcluder.Object});
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns(filePath);
            autoMocker.Setup<ITextInfoFactory, ITextInfo>(textInfoFactory => textInfoFactory.Create(It.IsAny<ITextView>(), It.IsAny<ITextBuffer>()))
                .Returns(mockTextInfo.Object);
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            var tagger = coverageTaggerProvider.CreateTagger(new Mock<ITextView>().Object, new Mock<ITextBuffer>().Object);

            Assert.That(tagger, isExcluded ? Is.Null : Is.Not.Null);
        }

        [TestCase]
        public void Should_Create_A_Coverage_Tagger_With_BufferLineCoverage_From_DynamicCoverageManager_And_Last_Coverage_Type_Filter_When_The_TextBuffer_Has_An_Associated_File_Document()
        {
            var textView = new Mock<ITextView>().Object;
            var textBuffer = new Mock<ITextBuffer>().Object;
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
            autoMocker.SetInstance(new IFileExcluder[0]);
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("file");
            autoMocker.Setup<ITextInfoFactory,ITextInfo>(textInfoFactory => textInfoFactory.Create(textView, textBuffer)).Returns(mockTextInfo.Object);    
            var bufferLineCoverage = new Mock<IBufferLineCoverage>().Object;
            autoMocker.GetMock<IDynamicCoverageManager>()
                .Setup(dynamicCoverageManager => dynamicCoverageManager.Manage(mockTextInfo.Object)).Returns(bufferLineCoverage);
            var coverageTaggerProvider = autoMocker.Create<CoverageTaggerProvider<DummyCoverageTypeFilter, DummyTag>>();

            var mockAppOptionsProvider = autoMocker.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, new AppOptions());

            var tagger = coverageTaggerProvider.CreateTagger(textView, textBuffer);

            Assert.That(tagger, Is.InstanceOf<CoverageTagger<DummyTag>>());

            var coverageTaggerType = typeof(CoverageTagger<DummyTag>);

            var fileLineCoverageArg = coverageTaggerType.GetField("bufferLineCoverage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as IBufferLineCoverage;
            var coverageTypeFilterArg = coverageTaggerType.GetField("coverageTypeFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as ICoverageTypeFilter;
            var textInfoArg = coverageTaggerType.GetField("textInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tagger) as ITextInfo;

            Assert.Multiple(() =>
            {
                Assert.That(textInfoArg, Is.SameAs(mockTextInfo.Object));
                Assert.That(fileLineCoverageArg, Is.SameAs(bufferLineCoverage));
                Assert.That(coverageTypeFilterArg, Is.SameAs(lastFilter));
            });
        }
    }
}