using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class JsThemeStyling
    {
//#pragma warning disable IDE0079 // Remove unnecessary suppression
//#pragma warning disable SA1401 // Fields should be private
        public string BackgroundColour;
        public string FontColour;
        public string TableBorderColour;
        public string LinkColour;
        public string CoverageTableHeaderFontColour;
        public string CoverageTableRowHoverBackgroundColour;
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
        public string GrayCoverage;
        public string ComboBox;
        public string ComboBoxBorder;
        public string ComboBoxText;
        public string ScrollBarArrow;
        public string ScrollBarTrack;
        public string ScrollBarThumb;
//#pragma warning restore SA1401 // Fields should be private
//#pragma warning restore IDE0079 // Remove unnecessary suppression
    }
}
