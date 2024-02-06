using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal class TextInfo
    {
        public TextInfo(ITextView textView, ITextBuffer textBuffer, string filePath)
        {
            TextView = textView;
            TextBuffer = textBuffer;
            FilePath = filePath;
        }

        public ITextView TextView { get; }
        public ITextBuffer TextBuffer { get; }
        public string FilePath { get; }
    }
}
