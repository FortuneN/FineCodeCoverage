using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    internal interface ITextFormattingRunPropertiesFactory
    {
        TextFormattingRunProperties Create(IFontAndColorsInfo fontAndColorsInfo);
    }
}
