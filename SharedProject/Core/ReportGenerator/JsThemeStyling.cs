using System.Security.Permissions;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class JsThemeStyling
    {
        public string BackgroundColour;
        public string FontColour;
        public string TableBorderColour;
        public string LinkColour;
        public string CoverageTableHeaderFontColour;
        public string CoverageTableRowHoverBackgroundColour;
        public string CoverageTableRowHoverColour;
        public string DivHeaderBackgroundColour;
        public string TabBackgroundColour;
        public string HeaderFontColour;
        public string HeaderBorderColour;
        public string TextBoxColour;
        public string TextBoxTextColour;
        public string TextBoxBorderColour;
        public string PlusBase64;
        public string MinusBase64;
        public string DownActiveBase64;
        public string DownInactiveBase64;
        public string UpActiveBase64;
        public string GrayCoverageColour;
        public string ComboBoxColour;
        public string ComboBoxBorderColour;
        public string ComboBoxTextColour;
        public string ScrollBarArrowColour;
        public string ScrollBarTrackColour;
        public string ScrollBarThumbColour;
        public string SliderLeftColour;
        public string SliderRightColour;
        public string SliderThumbColour;

        public string ButtonBorderColour;
        public string ButtonBorderDisabledColour;
        public string ButtonBorderFocusedColour;
        public string ButtonBorderHoverColour;
        public string ButtonBorderPressedColour;
        public string ButtonColour;
        public string ButtonDisabledColour;
        public string ButtonFocusedColour;
        public string ButtonHoverColour;
        public string ButtonPressedColour;
        public string ButtonTextColour;
        public string ButtonDisabledTextColour;
        public string ButtonFocusedTextColour;
        public string ButtonHoverTextColour;
        public string ButtonPressedTextColour;
    }
}
