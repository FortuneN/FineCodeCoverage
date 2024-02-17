using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class TextInfo
    {
        public TextInfo(ITextView textView, ITextBuffer textBuffer, string filePath)
        {
            TextView = textView;
            TextBuffer = textBuffer as ITextBuffer2;
            FilePath = filePath;
        }

        public ITextView TextView { get; }
        public ITextBuffer2 TextBuffer { get; }
        public string FilePath { get; }

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            return obj is TextInfo info &&
                   TextView == info.TextView &&
                   TextBuffer == info.TextBuffer &&
                   FilePath == info.FilePath;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            int hashCode = -5208965;
            hashCode = hashCode * -1521134295 + EqualityComparer<ITextView>.Default.GetHashCode(TextView);
            hashCode = hashCode * -1521134295 + EqualityComparer<ITextBuffer>.Default.GetHashCode(TextBuffer);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            return hashCode;
        }
    }
}
