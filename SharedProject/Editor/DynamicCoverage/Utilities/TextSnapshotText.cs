using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITextSnapshotText))]
    [ExcludeFromCodeCoverage]
    internal class TextSnapshotText : ITextSnapshotText
    {
        public string GetLineText(ITextSnapshot textSnapshot, int lineNumber)
            => textSnapshot.GetLineFromLineNumber(lineNumber).Extent.GetText();
    }
}
