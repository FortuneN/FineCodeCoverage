using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
    interface ITextFormattingRunPropertiesFactory
    {
        TextFormattingRunProperties Create(IFontAndColorsInfo fontAndColorsInfo);
    }
}
