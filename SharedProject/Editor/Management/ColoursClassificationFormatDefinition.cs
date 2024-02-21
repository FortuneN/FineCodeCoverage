using Microsoft.VisualStudio.Text.Classification;
using System.Windows;
using System.Windows.Media;

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
