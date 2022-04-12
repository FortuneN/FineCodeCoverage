using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(NotCoveredEditorFormatDefinition.ResourceName)]
    [UserVisible(true)]
    internal class NotCoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCNotCovered";

        [ImportingConstructor]
        public NotCoveredEditorFormatDefinition(
            IEditorFormatMapCoverageColoursManager editorFormatMapCoverageColoursManager
        ) : base(ResourceName, editorFormatMapCoverageColoursManager, CoverageType.NotCovered)
        {
        }
    }
}
