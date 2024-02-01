using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Impl
{
    internal interface ITextBufferWithFilePath
    {
        string FilePath { get; }
        ITextBuffer TextBuffer { get; }
    }
}
