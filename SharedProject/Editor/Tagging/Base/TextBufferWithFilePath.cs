using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal class TextBufferWithFilePath : ITextBufferWithFilePath
    {
        private readonly ITextDocument textDocument;

        public TextBufferWithFilePath(ITextBuffer textBuffer, ITextDocument textDocument)
        {
            ThrowIf.Null(textBuffer, nameof(textBuffer));
            ThrowIf.Null(textDocument, nameof(textDocument));
            TextBuffer = textBuffer;
            this.textDocument = textDocument;
        }
        public ITextBuffer TextBuffer { get; }
        public string FilePath => textDocument.FilePath;

    }
}
