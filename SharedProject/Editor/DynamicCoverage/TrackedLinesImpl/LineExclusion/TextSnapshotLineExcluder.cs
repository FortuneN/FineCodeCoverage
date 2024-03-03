using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;

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
            => this.codeLineExcluder.ExcludeIfNotCode(this.textSnapshotText.GetLineText(textSnapshot, lineNumber), isCSharp);
    }
}
