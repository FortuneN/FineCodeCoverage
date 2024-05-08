using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
        private Mock<IAppOptions> mockAppOptions;

        private ILine CreateLine()
        {
            var mockLine = new Mock<ILine>();
            mockLine.SetupGet(line => line.Number).Returns(1);
            mockLine.SetupGet(line => line.CoverageType).Returns(CoverageType.Partial);
            return mockLine.Object;
        }
        private void SetupEditorCoverageColouringMode(AutoMoqer autoMoqer,bool off = false)
        {
            mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(off ? EditorCoverageColouringMode.Off : EditorCoverageColouringMode.UseRoslynWhenTextChanges);
            autoMoqer.Setup<IAppOptionsProvider, IAppOptions>(appOptionsProvider => appOptionsProvider.Get()).Returns(mockAppOptions.Object);
        }
        private void SimpleTextInfoSetUp(bool editorCoverageOff = false,string contentTypeName = "CSharp")
        {
            autoMoqer = new AutoMoqer();
            SetupEditorCoverageColouringMode(autoMoqer, editorCoverageOff);
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

        [Test]
        public void Should_Create_Tracked_Lines_From_Existing_Coverage()
        {
            SimpleTextInfoSetUp(false,"CSharp");

            var lines = new List<ILine> { CreateLine()};
            var mockFileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>();
            mockFileLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines("filepath")).Returns(lines);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            autoMoqer.Verify<ITrackedLinesFactory>(trackedLinesFactory => trackedLinesFactory.Create(lines, textSnapshot, "filepath"));
        }

        [Test]
        public void Should_Not_Create_TrackedLines_If_EditorCoverageColouringMode_Is_Off()
        {
            SimpleTextInfoSetUp(true);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            autoMoqer.Verify<ITrackedLinesFactory>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(),It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void Should_Not_Create_TrackedLines_From_NewCoverageLinesMessage_If_EditorCoverageColouringMode_Is_Off()
        {
            SimpleTextInfoSetUp(true);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            var newCoverageLinesMessage = new NewCoverageLinesMessage { CoverageLines = new Mock<IFileLineCoverage>().Object };
            bufferLineCoverage.Handle(newCoverageLinesMessage);

            autoMoqer.Verify<ITrackedLinesFactory>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()), Times.Never());
        }

        // todo - check all other tests - should be verifying on GetLines and CoverageChangedMessage
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Not_Create_TrackedLines_From_NewCoverageLinesMessage_If_Text_Changed_Since_TestExecutionStartingMessage(
            bool textChangedSinceTestExecutionStarting
        )
        {
            var autoMoqer = new AutoMoqer();
            var mockTextView = new Mock<ITextView>();
            var mockTextBuffer = new Mock<ITextBuffer2>();
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            mockTextBuffer.Setup(textBuffer => textBuffer.CurrentSnapshot).Returns(textSnapshot);
            var mockTextInfo = new Mock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer).Returns(mockTextBuffer.Object);
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(mockTextView.Object);
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            autoMoqer.SetInstance(mockTextInfo.Object);

            var mockAppOptionsProvider = autoMoqer.GetMock<IAppOptionsProvider>();
            var firstMockAppOptions = new Mock<IAppOptions>();
            firstMockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(EditorCoverageColouringMode.Off);
            var secondAppOptions = new Mock<IAppOptions>();
            secondAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(EditorCoverageColouringMode.UseRoslynWhenTextChanges);
            mockAppOptionsProvider.SetupSequence(appOptionsProvider => appOptionsProvider.Get())
                .Returns(firstMockAppOptions.Object).Returns(secondAppOptions.Object);

            var mockTrackedLinesFactory = autoMoqer.GetMock<ITrackedLinesFactory>();
            var mockTrackedLines = new Mock<ITrackedLines>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(0, 10)).Returns(new List<IDynamicLine> { new Mock<IDynamicLine>().Object });
            mockTrackedLinesFactory.Setup(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), textSnapshot, "filepath"))
                .Returns(mockTrackedLines.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();
            void RaiseChangedOnBackground()
            {
                mockTextBuffer.Raise(textBuffer => textBuffer.ChangedOnBackground += null, new TextContentChangedEventArgs(new Mock<ITextSnapshot>().Object, new Mock<ITextSnapshot>().Object, new EditOptions(), null));
            }
            void RaiseTestExecutionStartingMessage()
            {
                (bufferLineCoverage as IListener<TestExecutionStartingMessage>).Handle(new TestExecutionStartingMessage());
            }
            

            if (textChangedSinceTestExecutionStarting)
            {
                RaiseTestExecutionStartingMessage();
                Thread.Sleep(1);
                RaiseChangedOnBackground();
            } else
            {
                RaiseChangedOnBackground();
                Thread.Sleep(1);
                RaiseTestExecutionStartingMessage();
            }

            bufferLineCoverage.Handle(new NewCoverageLinesMessage { CoverageLines = new Mock<IFileLineCoverage>().Object });
           

            Assert.That(bufferLineCoverage.GetLines(0,10).Count(), Is.EqualTo(textChangedSinceTestExecutionStarting ? 0 : 1));

            var expectCoverageChangedMessage = textChangedSinceTestExecutionStarting ? false : true;
            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(
                It.IsAny<CoverageChangedMessage>(), null), 
                Times.Exactly(expectCoverageChangedMessage ? 1 : 0));
        }

        [Test]
        public void Should_Not_Throw_If_No_Initial_Coverage()
        {
            SimpleTextInfoSetUp();
            
            new BufferLineCoverage(null, textInfo, new Mock<IEventAggregator>().Object, null, null,new Mock<IAppOptionsProvider>().Object);
        }

        [Test]
        public void GetLines_Should_Delegate_To_TrackedLines()
        {
            SimpleTextInfoSetUp();

            var mockTrackedLines = new Mock<ITrackedLines>();
            var lines = new List<IDynamicLine>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(lines);

            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
                .Returns(mockTrackedLines.Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            Assert.That(bufferLineCoverage.GetLines(2, 5), Is.SameAs(lines));
        }

        [Test]
        public void Should_Add_Itself_As_A_Listener()
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
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
                .Returns(new Mock<ITrackedLines>().Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.Handle(new NewCoverageLinesMessage());

            var lines = bufferLineCoverage.GetLines(2, 5);
            Assert.That(lines, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_New_TextLines_When_Coverage_Changed_And_Not_Off_In_AppOptions(bool off)
        {
            var autoMoqer = new AutoMoqer();
            SetupEditorCoverageColouringMode(autoMoqer, off);
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

            var lines = new List<ILine> { CreateLine()};
            var mockNewFileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>();
            mockNewFileLineCoverage.Setup(fileLineCoverage => fileLineCoverage.GetLines("filepath")).Returns(lines);

            var mockTrackedLines = new Mock<ITrackedLines>();
            var dynamicLines = new List<IDynamicLine>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(dynamicLines);
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(lines, mockCurrentSnapshot.Object, "filepath")
                ).Returns(mockTrackedLines.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            (bufferLineCoverage as IListener<NewCoverageLinesMessage>).Handle(new NewCoverageLinesMessage { CoverageLines = mockNewFileLineCoverage.Object });
            var bufferLines = bufferLineCoverage.GetLines(2, 5);
            if (off)
            {
                Assert.That(bufferLines, Is.Empty);
            }
            else
            {
                Assert.That(bufferLines, Is.SameAs(dynamicLines));
            }
        }

        [TestCase(true,true,true,true)]
        [TestCase(true, false, true, true)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, false, true)]
        [TestCase(false, false, false, false)]
        public void Should_Send_CoverageChangedMessage_When_Necessary(
            bool initialTrackedLines,
            bool nextTrackedLines,
            bool hasCoverageLines, 
            bool expectedSends)
        {
            SimpleTextInfoSetUp();
            var trackedLines = new Mock<ITrackedLines>().Object;
            var mockTrackedLinesFactory = autoMoqer.GetMock<ITrackedLinesFactory>();
            mockTrackedLinesFactory.SetupSequence(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
                .Returns(initialTrackedLines ? trackedLines : null)
                .Returns(nextTrackedLines ? trackedLines : null);
            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            var newCoverageLinesMessage = new NewCoverageLinesMessage();
            if(hasCoverageLines)
            {
                newCoverageLinesMessage.CoverageLines = new Mock<IFileLineCoverage>().Object;
            }
            bufferLineCoverage.Handle(newCoverageLinesMessage);

            autoMoqer.Verify<IEventAggregator>(
                eventAggregator => eventAggregator.SendMessage(
                    It.Is<CoverageChangedMessage>(message => message.AppliesTo == "filepath" && message.BufferLineCoverage == bufferLineCoverage)
                    , null
                ), expectedSends ? Times.Once() : Times.Never());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Update_TrackedLines_When_Text_Buffer_ChangedOnBackground(bool linesInRange)
        {
            SimpleTextInfoSetUp();

            var mockAfterSnapshot = new Mock<ITextSnapshot>();
            mockAfterSnapshot.SetupGet(textSnapshot => textSnapshot.LineCount).Returns(linesInRange ? 100 : 11);

            var newSpan = new Span(1, 2);
            var mockTrackedLines = new Mock<ITrackedLines>();
            var changedLineNumbers = new List<int> { 11, 12 };
            mockTrackedLines.Setup(trackedLines => trackedLines.GetChangedLineNumbers(mockAfterSnapshot.Object, new List<Span> { newSpan }))
                .Returns(changedLineNumbers);
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
                .Returns(mockTrackedLines.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            mockTextBuffer.Raise(textBuffer => textBuffer.ChangedOnBackground += null, CreateTextContentChangedEventArgs(mockAfterSnapshot.Object, newSpan));

            autoMoqer.Verify<IEventAggregator>(
                        eventAggregator => eventAggregator.SendMessage(
                            It.Is<CoverageChangedMessage>(message => message.AppliesTo == "filepath" && message.BufferLineCoverage == bufferLineCoverage && message.ChangedLineNumbers.SequenceEqual(changedLineNumbers))
                            , null
                        ), Times.Exactly(linesInRange ? 1 : 0));

        }

        [Test]
        public void Should_Not_Throw_When_Text_Buffer_Changed_And_No_Coverage()
        {
            SimpleTextInfoSetUp();

            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>()))
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
            mockTextView.Setup(textView => textView.TextSnapshot.GetText()).Returns("");
            mockTextView.Raise(textView => textView.Closed += null, EventArgs.Empty);

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.RemoveListener(bufferLineCoverage));
            mockTextView.VerifyRemove(textView => textView.Closed -= It.IsAny<EventHandler>(), Times.Once);
            mockTextBuffer.VerifyRemove(textBuffer => textBuffer.Changed -= It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Once);
            var mockAppOptionsProvider = autoMoqer.GetMock<IAppOptionsProvider>();
            mockAppOptionsProvider.VerifyRemove(appOptionsProvider => appOptionsProvider.OptionsChanged -= It.IsAny<Action<IAppOptions>>(), Times.Once);
        }

        [Test]
        public void Should_SaveSerializedCoverage_When_TextView_Closed_And_There_Has_Been_Coverage()
        {
            var autoMoqer = new AutoMoqer();
            SetupEditorCoverageColouringMode(autoMoqer);
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.ContentType.TypeName).Returns("contenttypename");
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.CurrentSnapshot).Returns(new Mock<ITextSnapshot>().Object);
            var mockTextView = new Mock<ITextView>();
            mockTextView.Setup(textView => textView.TextSnapshot.GetText()).Returns("text");
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(mockTextView.Object);
            autoMoqer.Setup<IFileLineCoverage, IEnumerable<ILine>>(
                fileLineCoverage => fileLineCoverage.GetLines(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())
            ).Returns(new List<ILine> { });
            var trackedLines = new Mock<ITrackedLines>().Object;
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>())
            ).Returns(trackedLines);
            autoMoqer.Setup<ITrackedLinesFactory, string>(
                trackedLinesFactory => trackedLinesFactory.Serialize(trackedLines, "text")
            ).Returns("serialized");

            autoMoqer.Create<BufferLineCoverage>();

            mockTextView.Raise(textView => textView.Closed += null, EventArgs.Empty);

            autoMoqer.Verify<IDynamicCoverageStore>(dynamicCoverageStore => dynamicCoverageStore.SaveSerializedCoverage("filepath", "serialized"));
        }

        [Test]
        public void Should_Remove_Serialized_Coverage_When_TextView_Closed_And_No_TrackedLines()
        {
            var autoMoqer = new AutoMoqer();
            SetupEditorCoverageColouringMode(autoMoqer,true);
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer).Returns(new Mock<ITextBuffer2>().Object);
            var mockTextView = new Mock<ITextView>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(mockTextView.Object);
            mockTextView.Setup(textView => textView.TextSnapshot.GetText()).Returns("");
            
            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();
            

            mockTextView.Raise(textView => textView.Closed += null, EventArgs.Empty);

            autoMoqer.Verify<IDynamicCoverageStore>(dynamicCoverageStore => dynamicCoverageStore.RemoveSerializedCoverage("filepath"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Create_From_Serialized_Coverage_If_Present(bool hasSerialized)
        {
            var autoMoqer = new AutoMoqer();
            SetupEditorCoverageColouringMode(autoMoqer);
            var mockTextInfo = autoMoqer.GetMock<ITextInfo>();
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.ContentType.TypeName).Returns("contentTypeLanguage");
            mockTextInfo.SetupGet(textInfo => textInfo.TextView).Returns(new Mock<ITextView>().Object);
            var currentTextSnaphot = new Mock<ITextSnapshot>().Object;
            mockTextInfo.SetupGet(textInfo => textInfo.TextBuffer.CurrentSnapshot).Returns(currentTextSnaphot);
            mockTextInfo.SetupGet(textInfo => textInfo.FilePath).Returns("filepath");
            autoMoqer.Setup<IDynamicCoverageStore,string>(dynamicCoverageStore => dynamicCoverageStore.GetSerializedCoverage("filepath"))
                .Returns(hasSerialized ? "serialized" : null);

            var mockTrackedLinesNoSerialized = new Mock<ITrackedLines>();
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>())
            ).Returns(mockTrackedLinesNoSerialized.Object);

            var mockTrackedLinesFromSerialized = new Mock<ITrackedLines>();
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedLinesFactory => trackedLinesFactory.Create("serialized", currentTextSnaphot, "filepath")
            ).Returns(mockTrackedLinesFromSerialized.Object);


            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            bufferLineCoverage.GetLines(2, 5);

            var expectedMockTrackedLines = hasSerialized ? mockTrackedLinesFromSerialized : mockTrackedLinesNoSerialized;
            expectedMockTrackedLines.Verify(trackedLines => trackedLines.GetLines(2, 5), Times.Once);
        }

        [Test]
        public void Should_Remove_TrackedLines_When_AppOptions_Changed_And_EditorCoverageColouringMode_Is_Off()
        {
            SimpleTextInfoSetUp();
            var mockTrackedLines = new Mock<ITrackedLines>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<IDynamicLine> { new Mock<IDynamicLine>().Object });
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>())
            ).Returns(mockTrackedLines.Object);
            
            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();
            Assert.That(bufferLineCoverage.GetLines(2, 5).Count(), Is.EqualTo(1));

            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(EditorCoverageColouringMode.Off);
            autoMoqer.GetMock<IAppOptionsProvider>().Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, mockAppOptions.Object);

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(It.IsAny<CoverageChangedMessage>(), null));

            Assert.That(bufferLineCoverage.GetLines(2, 5), Is.Empty);
        }

        [Test]
        public void Should_Not_Remove_TrackedLines_When_AppOptions_Changed_And_EditorCoverageColouringMode_Is_Not_Off()
        {
            SimpleTextInfoSetUp();
            var mockTrackedLines = new Mock<ITrackedLines>();
            mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<IDynamicLine> { new Mock<IDynamicLine>().Object });
            autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
                trackedCoverageLinesFactory => trackedCoverageLinesFactory.Create(It.IsAny<List<ILine>>(), It.IsAny<ITextSnapshot>(), It.IsAny<string>())
            ).Returns(mockTrackedLines.Object);

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();
            Assert.That(bufferLineCoverage.GetLines(2, 5).Count(), Is.EqualTo(1));

            var mockAppOptions = new Mock<IAppOptions>();
            mockAppOptions.SetupGet(appOptions => appOptions.EditorCoverageColouringMode).Returns(EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges);
            autoMoqer.GetMock<IAppOptionsProvider>().Raise(appOptionsProvider => appOptionsProvider.OptionsChanged += null, mockAppOptions.Object);

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.SendMessage(It.IsAny<CoverageChangedMessage>(), null), Times.Never());

            Assert.That(bufferLineCoverage.GetLines(2, 5).Count(), Is.EqualTo(1));
        }

    }
}
