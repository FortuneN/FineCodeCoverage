using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(CoveredEditorFormatDefinition.ResourceName)]
    [UserVisible(true)]
    internal class CoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCCovered";
        [ImportingConstructor]
        public CoveredEditorFormatDefinition(
            IEditorFormatMapCoverageColoursManager editorFormatMapCoverageColoursManager
        ) : base(ResourceName, editorFormatMapCoverageColoursManager, CoverageType.Covered)
        {
        }
    }
}
