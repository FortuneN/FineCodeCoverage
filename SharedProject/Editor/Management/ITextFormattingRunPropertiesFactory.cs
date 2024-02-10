using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    interface ITextFormattingRunPropertiesFactory
    {
        TextFormattingRunProperties Create(IFontAndColorsInfo fontAndColorsInfo);
    }
}
