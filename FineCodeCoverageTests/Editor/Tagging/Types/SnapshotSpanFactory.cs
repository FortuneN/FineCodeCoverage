using Microsoft.VisualStudio.Text;
using Moq;

namespace FineCodeCoverageTests.Editor.Tagging.Types
{
    public static class SnapshotSpanFactory
    {
        public static SnapshotSpan Create(int end)
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(textSnapshot => textSnapshot.Length).Returns(end + 1);
            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, end));
        }
    }
}