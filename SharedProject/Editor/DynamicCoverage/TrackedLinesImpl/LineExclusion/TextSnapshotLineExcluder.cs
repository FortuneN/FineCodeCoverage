using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ITextSnapshotLineExcluder))]
    internal class TextSnapshotLineExcluder : ITextSnapshotLineExcluder
    {
        private readonly ITextSnapshotText textSnapshotText;
        private readonly ILineExcluder codeLineExcluder;

        [ImportingConstructor]
        public TextSnapshotLineExcluder(ITextSnapshotText textSnapshotText, ILineExcluder codeLineExcluder)
        {
            this.textSnapshotText = textSnapshotText;
            this.codeLineExcluder = codeLineExcluder;
        }
        public bool ExcludeIfNotCode(ITextSnapshot textSnapshot, int lineNumber, bool isCSharp)
        {
            return codeLineExcluder.ExcludeIfNotCode(textSnapshotText.GetLineText(textSnapshot, lineNumber), isCSharp);
        }
    }
}
