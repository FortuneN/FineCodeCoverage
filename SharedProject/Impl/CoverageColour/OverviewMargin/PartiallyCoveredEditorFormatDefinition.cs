using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(PartiallyCoveredEditorFormatDefinition.ResourceName)]
    [UserVisible(true)]
    internal class PartiallyCoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCPartial";
        [ImportingConstructor]
        public PartiallyCoveredEditorFormatDefinition(
            IEditorFormatMapCoverageColoursManager editorFormatMapCoverageColoursManager
        ) : base(ResourceName,editorFormatMapCoverageColoursManager,CoverageType.Partial)
        {
        }
    }
}
