using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Editor.Management
{
    // todo - consider a MEF export to allow other extensions to change the formatting
    [Export(typeof(ITextFormattingRunPropertiesFactory))]
    internal class TextFormattingRunPropertiesFactory : ITextFormattingRunPropertiesFactory
    {
        public TextFormattingRunProperties Create(IFontAndColorsInfo fontAndColorsInfo)
        {
            IItemCoverageColours coverageColours = fontAndColorsInfo.ItemCoverageColours;
            return TextFormattingRunProperties.CreateTextFormattingRunProperties(
                new SolidColorBrush(coverageColours.Foreground), new SolidColorBrush(coverageColours.Background),
                null, // Typeface
                null, // size
                null, // hinting size
               /*
                   TextDecorationCollection
                    https://docs.microsoft.com/en-us/dotnet/api/system.windows.textdecorations?view=windowsdesktop-8.0
                    https://learn.microsoft.com/en-us/dotnet/api/system.windows.textdecorations?view=windowsdesktop-8.0
               */
               null,
                // TextEffectCollection https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.texteffect?view=windowsdesktop-8.0
                null, // 
                null // CultureInfo
                ).SetBold(fontAndColorsInfo.IsBold);
        }
    }
}
