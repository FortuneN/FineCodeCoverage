using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal class TextBufferWithFilePath : ITextBufferWithFilePath
    {
        public TextBufferWithFilePath(ITextBuffer textBuffer, string filePath)
        {
            ThrowIf.Null(textBuffer, nameof(textBuffer));
            ThrowIf.Null(filePath, nameof(filePath));
            TextBuffer = textBuffer;
            FilePath = filePath;
        }
        public ITextBuffer TextBuffer { get; }
        public string FilePath { get; }

    }
}
