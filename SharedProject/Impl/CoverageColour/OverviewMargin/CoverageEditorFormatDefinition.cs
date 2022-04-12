using Microsoft.VisualStudio.Text.Classification;
using System.Windows.Media;

namespace FineCodeCoverage.Impl
{
    internal abstract class CoverageEditorFormatDefinition : EditorFormatDefinition, ICoverageEditorFormatDefinition
    {
        public CoverageEditorFormatDefinition(
            string identifier,
            IEditorFormatMapCoverageColoursManager editorFormatMapCoverageColoursManager,
            CoverageType coverageType)
        {
            Identifier = identifier;
            CoverageType = coverageType;
            editorFormatMapCoverageColoursManager.Register(this);
        }

        public string Identifier { get; private set; }

        public void SetBackgroundColor(Color backgroundColor)
        {
            BackgroundColor = backgroundColor;
        }

        public CoverageType CoverageType { get; }
    }

}
