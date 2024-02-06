using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FineCodeCoverageTests.Coverage_Colours
{
    internal class BufferLineCoverage_Tests
    {
        private AutoMoqer autoMoqer;
        private ITextSnapshot textSnapshot;
        private void SimpleTextInfoSetUp()
        {
            autoMoqer = new AutoMoqer();
            var mockTextBuffer = new Mock<ITextBuffer>();
            textSnapshot = new Mock<ITextSnapshot>().Object;
            mockTextBuffer.Setup(textBuffer => textBuffer.CurrentSnapshot).Returns(textSnapshot);
            var textInfo = new TextInfo(
                new Mock<ITextView>().Object,
                mockTextBuffer.Object,
                "filepath"
            );
            autoMoqer.SetInstance(textInfo);
        }

        [Test]
        public void Should_Create_Tracked_Lines_From_Existing_Coverage()
        {
            throw new NotImplementedException();
            //SimpleTextInfoSetUp();
            //var fileLineCoverage = autoMoqer.GetMock<IFileLineCoverage>().Object;

            //var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            //autoMoqer.Verify<ITrackedLinesFactory>(trackedLinesFactory => trackedLinesFactory.Create(fileLineCoverage, textSnapshot,"filepath"));
        }

        [Test]
        public void GetLines_Should_Delegate_To_TrackedLines()
        {
            throw new NotImplementedException();
            //SimpleTextInfoSetUp();
            //var mockTrackedLines = new Mock<ITrackedLines>();
            //var lines = new List<ILine>();
            //mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(lines);
            //autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<IFileLineCoverage>(), It.IsAny<ITextSnapshot>(),"filepath")).Returns(mockTrackedLines.Object);

            //var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            //Assert.That(bufferLineCoverage.GetLines(2, 5),Is.SameAs(lines));
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

            var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();
            bufferLineCoverage.Handle(new NewCoverageLinesMessage());
            var lines = bufferLineCoverage.GetLines(2, 5);
            
            Assert.That(lines, Is.Empty);

        }

        [Test]
        public void Should_Create_New_TextLines_When_Coverage_Changed()
        {
            throw new NotImplementedException();
            //var autoMoqer = new AutoMoqer();
            //var mockTextBuffer = new Mock<ITextBuffer>();
            //var currentSnapshot = new Mock<ITextSnapshot>().Object;
            //mockTextBuffer.SetupSequence(textBuffer => textBuffer.CurrentSnapshot)
            //    .Returns(new Mock<ITextSnapshot>().Object)
            //    .Returns(currentSnapshot);
            //var textInfo = new TextInfo(
            //    new Mock<ITextView>().Object,
            //    mockTextBuffer.Object,
            //    "filepath"
            //);
            //autoMoqer.SetInstance(textInfo);

            //var newFileLineCoverage = new Mock<IFileLineCoverage>().Object;

            //var mockTrackedLines = new Mock<ITrackedLines>();
            //var lines = new List<ILine>();
            //mockTrackedLines.Setup(trackedLines => trackedLines.GetLines(2, 5)).Returns(lines);
            //autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(
            //    trackedLinesFactory => trackedLinesFactory.Create(newFileLineCoverage, currentSnapshot,"filepath")
            //    ).Returns(mockTrackedLines.Object);


            //var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            //bufferLineCoverage.Handle(new NewCoverageLinesMessage { CoverageLines = newFileLineCoverage});

            //Assert.That(bufferLineCoverage.GetLines(2, 5), Is.SameAs(lines));

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
        public void Should_Update_TrackedLines_When_Text_Buffer_Changed(bool textLinesChanged)
        {
            throw new NotImplementedException();
            //autoMoqer = new AutoMoqer();
            //var mockTextBuffer = new Mock<ITextBuffer>();
            //textSnapshot = new Mock<ITextSnapshot>().Object;
            //mockTextBuffer.Setup(textBuffer => textBuffer.CurrentSnapshot).Returns(textSnapshot);
            //var textInfo = new TextInfo(
            //    new Mock<ITextView>().Object,
            //    mockTextBuffer.Object,
            //    "filepath"
            //);
            //autoMoqer.SetInstance(textInfo);

            //var afterSnapshot = new Mock<ITextSnapshot>().Object;

            //var mockTrackedLines = new Mock<ITrackedLines>();
            //mockTrackedLines.Setup(trackedLines => trackedLines.Changed(afterSnapshot,null)).Returns(textLinesChanged);
            //autoMoqer.Setup<ITrackedLinesFactory, ITrackedLines>(trackedLinesFactory => trackedLinesFactory.Create(It.IsAny<IFileLineCoverage>(), It.IsAny<ITextSnapshot>(), "filepath"))
            //    .Returns(mockTrackedLines.Object);


            //var bufferLineCoverage = autoMoqer.Create<BufferLineCoverage>();

            //mockTextBuffer.Raise(textBuffer => textBuffer.Changed += null, new TextContentChangedEventArgs(new Mock<ITextSnapshot>().Object,afterSnapshot,EditOptions.None,null));

            //autoMoqer.Verify<IEventAggregator>(
            //            eventAggregator => eventAggregator.SendMessage(
            //                It.Is<CoverageChangedMessage>(message => message.AppliesTo == "filepath" && message.CoverageLines == bufferLineCoverage)
            //                , null
            //            ),Times.Exactly(textLinesChanged ? 1 : 0));

        }
    }
}
