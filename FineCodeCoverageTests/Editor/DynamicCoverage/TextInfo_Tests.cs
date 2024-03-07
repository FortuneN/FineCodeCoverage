using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TextInfo_Tests
    {
        [Test]
        public void Should_Return_The_Current_FilePath_Each_Time()
        {
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.SetupSequence(textDocument => textDocument.FilePath).Returns("file1").Returns("file2");
            var mockTextBuffer = new Mock<ITextBuffer2>();
            var propertyCollection = new PropertyCollection();
            propertyCollection.AddProperty(typeof(ITextDocument), mockTextDocument.Object);
            mockTextBuffer.SetupGet(textBuffer => textBuffer.Properties).Returns(propertyCollection);

            var textInfo = new TextInfo(new Mock<ITextView>().Object, mockTextBuffer.Object);
            
            Assert.That(textInfo.FilePath, Is.EqualTo("file1"));
            Assert.That(textInfo.FilePath, Is.EqualTo("file2"));

        }

        [Test]
        public void Should_Have_Null_File_Path_When_No_TextDocument_In_Properties()
        {
            var mockTextBuffer = new Mock<ITextBuffer2>();
            var propertyCollection = new PropertyCollection();
            mockTextBuffer.SetupGet(textBuffer => textBuffer.Properties).Returns(propertyCollection);

            var textInfo = new TextInfo(new Mock<ITextView>().Object, mockTextBuffer.Object);

            Assert.That(textInfo.FilePath, Is.Null);
        }

        [Test]
        public void Should_Have_TextView_TextBuffer_Properties()
        {
            var textBuffer = new Mock<ITextBuffer2>().Object;
            var textView = new Mock<ITextView>().Object;
            var textInfo = new TextInfo(textView, textBuffer);

            Assert.That(textInfo.TextView, Is.SameAs(textView));
            Assert.That(textInfo.TextBuffer, Is.SameAs(textBuffer));
        }

    }
}
