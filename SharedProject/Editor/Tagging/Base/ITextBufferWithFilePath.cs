using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ITextBufferWithFilePath
    {
        string FilePath { get; }
        ITextBuffer TextBuffer { get; }
    }
}
