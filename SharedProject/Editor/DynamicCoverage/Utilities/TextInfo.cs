using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TextInfo : ITextInfo
    {
        private bool triedGetProperty;
        private ITextDocument document;
        private ITextDocument TextDocument
        {
            get
            {
                if (!triedGetProperty)
                {
                    triedGetProperty = true;
                    if (TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
                    {
                        this.document = document;
                    }
                }
                return document;
            }
        }
        public TextInfo(ITextView textView, ITextBuffer textBuffer)
        {
            TextView = textView;
            TextBuffer = textBuffer as ITextBuffer2;
        }

        public ITextView TextView { get; }
        public ITextBuffer2 TextBuffer { get; }
        public string FilePath
        {
            get
            {
                if (TextDocument != null)
                {
                    return TextDocument.FilePath;
                }
                return null;
            }
        }
    }
}
