using AutoMoq;
using FineCodeCoverage.Impl;
using Microsoft.VisualStudio.Text.Classification;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FineCodeCoverageTests.Coverage_Colours
{
    public class EditorFormatMapTextSpecificListener_Tests
    {
        [TestCase(new string[] { "This" }, new string[] { "That" }, false)]
        [TestCase(new string[] { "Other", "Match" }, new string[] { "NoMatch", "Match" }, true)]
        public void X(string[] listenFor, string[] changedItems, bool expectedInvocation)
        {
            var autoMoqer = new AutoMoqer();
            var mockEditorFormatMap = new Mock<IEditorFormatMap>();
            autoMoqer.Setup<IEditorFormatMapService, IEditorFormatMap>(editorFormatMapService => editorFormatMapService.GetEditorFormatMap("text"))
                .Returns(mockEditorFormatMap.Object);
            var editorFormatTextSpecificListener = autoMoqer.Create<EditorFormatMapTextSpecificListener>();
            var invoked = false;
            editorFormatTextSpecificListener.ListenFor(listenFor.ToList(), () =>
            {
                invoked = true;
            });
            mockEditorFormatMap.Raise(editorFormatMap => editorFormatMap.FormatMappingChanged += null, new FormatItemsEventArgs(new ReadOnlyCollection<string>(changedItems)));

            Assert.That(invoked, Is.EqualTo(expectedInvocation));
        }

        [Test]
        public void Should_Pause_Listening_When_Executing()
        {
            var autoMoqer = new AutoMoqer();
            var mockEditorFormatMap = new Mock<IEditorFormatMap>();
            autoMoqer.Setup<IEditorFormatMapService, IEditorFormatMap>(editorFormatMapService => editorFormatMapService.GetEditorFormatMap("text"))
                .Returns(mockEditorFormatMap.Object);
            var editorFormatTextSpecificListener = autoMoqer.Create<EditorFormatMapTextSpecificListener>();
            var invoked = false;
            editorFormatTextSpecificListener.ListenFor(new List<string> { "Match" }, () =>
            {
                invoked = true;
            });
            editorFormatTextSpecificListener.PauseListeningWhenExecuting(() =>
            {
                mockEditorFormatMap.Raise(editorFormatMap => editorFormatMap.FormatMappingChanged += null, new FormatItemsEventArgs(new ReadOnlyCollection<string>(new string[] { "Match" })));
            });

            Assert.That(invoked, Is.False);
        }

    }
}