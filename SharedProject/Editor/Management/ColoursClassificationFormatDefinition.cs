using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;

namespace FineCodeCoverage.Editor.Management
{
    internal class ColoursClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public ColoursClassificationFormatDefinition(Color foregroundColor, Color backgroundColor)
        {
            this.ForegroundColor = foregroundColor;
            this.BackgroundColor = backgroundColor;
        }
    }
}
