using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class NormalizedTextChangeCollection : List<ITextChange>, INormalizedTextChangeCollection
    {
        public bool IncludesLineChanges => throw new NotImplementedException();
    }

    internal class BufferLineCoverage_Tests
    {
        private AutoMoqer autoMoqer;
        private Mock<ITextSnapshot> mockTextSnapshot;
        private Mock<ITextBuffer2> mockTextBuffer;
        private Mock<ITextView> mockTextView;
        private ITextSnapshot textSnapshot;
        private ITextInfo textInfo;
        private void SimpleTextInfoSetUp(string contentTypeName = "CSharp")
        {
            autoMoqer = new AutoMoqer();
            mockTextView = new Mock<ITextView>();
            mockTextBuffer = new Mock<ITextBuffer2>();
            mockTextBuffer.Setup(textBuffer => textBuffer.ContentType.TypeName).Returns(contentTypeName);
            mockTextSnapshot = new Mock<ITextSnapshot>();
            textSnapshot = mockTextSnapshot.Object;
            mockTextBuffer.Setup(textBuffer => textBuffer.CurrentSnapshot).Returns(textSnapshot);
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer).Returns(mockTextBuffer.Object);
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(mockTextView.Object);
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            textInfo = mockTextInfo.Object;
            autoMoqer.SetInstance(textInfo);
        }

        [TestCase("CSharp", Language.CSharp)]
        [TestCase("Basic", Language.VB)]
        [TestCase("C/C++", Language.CPP)]
        public void Should_Create_Tracked_Lines_From_Existing_Coverage_Based_Upon_The_Content_Type_Language(string contentTypeName, Language expectedLanguage)
        {
            SimpleTextInfoSetUp(contentTypeName);
            mockTextSnapshot.Setup(snapshot => snapshot.LineCount).Returns(5);

            var lines = new List<ILine> { };
            var mockFileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>();
            mockFileLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines("filepath", 1, 6)).Returns(lines);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            autoMoqer.Verify<ITrackedLinesFactory>(trackedLinesFactory => trackedLinesFactory.Create(lines, textSnapshot, expectedLanguage));
        }

        [Test]
        public void Should_Not_Throw_If_No_Initial_Coverage()
        {
            SimpleTextInfoSetUp();
            new BufferLineCoverage(null, textInfo, new Mock<IEventAggregator>().Object, null, null);
        }

        [Test]
        public void GetLines_Should_Delegate_To_TrackedLines()
        {
            SimpleTextInfoSetUp();

            var mockTrackedLines = new Mock<ITrackedLines>();
            var lines = new List<IDynamicLine>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(lines);

            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>()))
                .Returns(mockTrackedLines.Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            Assert.That(bufferLineCoverage.GetLines(2, 5), Is.SameAs(lines));
        }

        [Test]
        public void Should_Listen_For_Coverage_Changed()
        {
            SimpleTextInfoSetUp();

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(bufferLineCoverage, null));
        }

        [Test]
        public void Should_Have_Empty_Lines_When_Coverage_Cleared()
        {
            SimpleTextInfoSetUp();

            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>()))
                .Returns(new Mock<ITrackedLines>().Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.Handle(new NewCoverageLinesMessage());

            var lines = bufferLineCoverage.GetLines(2, 5);
            Assert.That(lines, Is.Empty);
        }

        [Test]
        public void Should_Create_New_TextLines_When_Coverage_Changed()
        {
            var autoMoqer = new AutoMoqer();
            var mockTextBuffer = new Mock<ITextBuffer2>();
            mockTextBuffer.Setup(textBuffer => textBuffer.ContentType.TypeName).Returns("CSharp");
            var mockCurrentSnapshot = new Mock<ITextSnapshot>();
            mockCurrentSnapshot.SetupGet(snapshot => snapshot.LineCount).Returns(10);
            mockTextBuffer.SetupSequence(textBuffer => textBuffer.CurrentSnapshot)
                .Returns(new Mock<ITextSnapshot>().Object)
                .Returns(mockCurrentSnapshot.Object);
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.SetupGet(textDocument => textDocument.FilePath).Returns("filepath");
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer).Returns(mockTextBuffer.Object);
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(new Mock<ITextView>().Object);
            autoMoqer.SetInstance(mockTextInfo.Object);

            var lines = new List<ILine> { };
            var mockNewFileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>();
            mockNewFileLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines("filepath", 1, 11)).Returns(lines);

            var mockTrackedLines = new Mock<ITrackedLines>();
            var dynamicLines = new List<IDynamicLine>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(dynamicLines);
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(lines, mockCurrentSnapshot.Object, Language.CSharp)
                ).Returns(mockTrackedLines.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.Handle(new NewCoverageLinesMessage { CoverageLines = mockNewFileLineCoverage.Object });

            Assert.That(bufferLineCoverage.GetLines(2, 5), Is.SameAs(dynamicLines));

        }

        [Test]
        public void Should_Send_CoverageChangedMessage_When_Coverage_Changed()
        {
            SimpleTextInfoSetUp();

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.Handle(new NewCoverageLinesMessage());

            autoMoqer.Verify<IEventAggregator>(
                eventAggregator => eventAggregator.SendMessage(It.Is<CoverageChangedMessage>(message => message.AppliesTo == "filepath" && message.CoverageLines == bufferLineCoverage), null));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_TrackedLines_When_Text_Buffer_ChangedOnBackground(bool textLinesChanged)
        {
            SimpleTextInfoSetUp();

            var afterSnapshot = new Mock<ITextSnapshot>().Object;

            var newSpan = new Span(1, 2);
            var mockTrackedLines = new Mock<ITrackedLines>();
            mockTrackedLines.Setup(trackedLines => trackedLines.Changed(afterSnapshot, new List<Span> { newSpan })).Returns(textLinesChanged);
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>()))
                .Returns(mockTrackedLines.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            mockTextBuffer.Raise(textBuffer => textBuffer.ChangedOnBackground += null, CreateTextContentChangedEventArgs(afterSnapshot, newSpan));

            autoMoqer.Verify<IEventAggregator>(
                        eventAggregator => eventAggregator.SendMessage(
                            It.Is<CoverageChangedMessage>(message => message.AppliesTo == "filepath" && message.CoverageLines == bufferLineCoverage)
                            , null
                        ), Times.Exactly(textLinesChanged ? 1 : 0));

        }

        [Test]
        public void Should_Not_Throw_When_Text_Buffer_Changed_And_No_Coverage()
        {
            SimpleTextInfoSetUp();

            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>()))
                .Returns(new Mock<ITrackedLines>().Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            // clear coverage
            bufferLineCoverage.Handle(new NewCoverageLinesMessage());

            mockTextBuffer.Raise(textBuffer => textBuffer.Changed += null, CreateTextContentChangedEventArgs(new Mock<ITextSnapshot>().Object, new Span(0, 0)));
        }

        private TextContentChangedEventArgs CreateTextContentChangedEventArgs(ITextSnapshot afterSnapshot, params Span[] newSpans)
        {
            var normalizedTextChangeCollection = new NormalizedTextChangeCollection();
            foreach (var newSpan in newSpans)
            {
                var mockTextChange = new Mock<ITextChange>();
                mockTextChange.SetupGet(textChange => textChange.NewSpan).Returns(newSpan);
                normalizedTextChangeCollection.Add(mockTextChange.Object);
            };

            var mockBeforeSnapshot = new Mock<ITextSnapshot>();
            mockBeforeSnapshot.Setup(snapshot => snapshot.Version.Changes).Returns(normalizedTextChangeCollection);
            return new TextContentChangedEventArgs(mockBeforeSnapshot.Object, afterSnapshot, EditOptions.None, null);
        }

        [Test]
        public void Should_Stop_Listening_When_TextView_Closed()
        {
            SimpleTextInfoSetUp();

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            mockTextView.Raise(textView => textView.Closed += null, EventArgs.Empty);

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.RemoveListener(bufferLineCoverage));
            mockTextView.VerifyRemove(textView => textView.Closed -= It.IsAny<EventHandler>(), Times.Once);
            mockTextBuffer.VerifyRemove(textBuffer => textBuffer.Changed -= It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Once);
        }

        [Test]
        public void Should_SaveSerializedCoverage_When_TextView_Closed_And_There_Has_Been_Coverage()
        {
            var autoMoqer = new AutoMoqer();
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.ContentType.TypeName).Returns("contenttypename");
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.CurrentSnapshot).Returns(new Mock<ITextSnapshot>().Object);
            var mockTextView = new Mock<ITextView>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(mockTextView.Object);
            autoMoqer.Setup<IFileLineCoverage,IEnumerable<ILine>>(
                fileLineCoverage => fileLineCoverage.GetLines(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())
            ).Returns(new List<ILine> { });
            var trackedLines = new Mock<ITrackedLines>().Object;
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>())
            ).Returns(trackedLines);
            autoMoqer.Setup<ITrackedLinesFactory, string>(
                trackedLinesFactory => trackedLinesFactory.Serialize(trackedLines)
            ).Returns("serialized");


            autoMoqer.Create<BufferLineCoverage>();
            
            mockTextView.Raise(textView => textView.Closed += null, EventArgs.Empty);

            autoMoqer.Verify<IDynamicCoverageStore>(dynamicCoverageStore => dynamicCoverageStore.SaveSerializedCoverage("filepath", "serialized"));
            

        }

        [TestCase(true, "CSharp", Language.CSharp)]
        [TestCase(true, "Basic", Language.VB)]
        [TestCase(true, "C/C++", Language.CPP)]
        [TestCase(false,"",Language.CPP)]
        public void Should_Create_From_Serialized_Coverage_If_Present(bool hasSerialized,string contentTypeLanguage, Language expectedLanguage)
        {
            var autoMoqer = new AutoMoqer();
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.ContentType.TypeName).Returns(contentTypeLanguage);
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(new Mock<ITextView>().Object);
            var currentTextSnaphot = new Mock<ITextSnapshot>().Object;
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.CurrentSnapshot).Returns(currentTextSnaphot);
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            autoMoqer.Setup<IDynamicCoverageStore,string>(dynamicCoverageStore => dynamicCoverageStore.GetSerializedCoverage("filepath"))
                .Returns(hasSerialized ? "serialized" : null);

            var mockTrackedLinesNoSerialized = new Mock<ITrackedLines>();
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<Language>())
            ).Returns(mockTrackedLinesNoSerialized.Object);

            var mockTrackedLinesFromSerialized = new Mock<ITrackedLines>();
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create("serialized", currentTextSnaphot, expectedLanguage)
            ).Returns(mockTrackedLinesFromSerialized.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.GetLines(2, 5);

            var expectedMockTrackedLines = hasSerialized ? mockTrackedLinesFromSerialized : mockTrackedLinesNoSerialized;
            expectedMockTrackedLines.Verify(trackedLines => trackedLines.GetLines(2, 5), Times.Once);
        }
    }
}
