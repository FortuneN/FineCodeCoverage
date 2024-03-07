using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class TextSnapshotLineExcluder_Tests
    {
        [TestCase(true,true)]
        [TestCase(false,true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void Should_Delegate(bool isCSharp, bool exclude)
        {
            var textSnapshot = new Mock<ITextSnapshot>().Object;
            var mockTextSnapshotText = new Mock<ITextSnapshotText>();
            mockTextSnapshotText.Setup(textSnapshotText => textSnapshotText.GetLineText(textSnapshot, 1)).Returns("line text");
            var mockLineExcluder = new Mock<ILineExcluder>();
            mockLineExcluder.Setup(lineExcluder => lineExcluder.ExcludeIfNotCode("line text", isCSharp)).Returns(exclude);
            var textSnapshotLineExcluder = new TextSnapshotLineExcluder(mockTextSnapshotText.Object, mockLineExcluder.Object);

            Assert.That(textSnapshotLineExcluder.ExcludeIfNotCode(textSnapshot, 1, isCSharp), Is.EqualTo(exclude));
        }
    }
}
