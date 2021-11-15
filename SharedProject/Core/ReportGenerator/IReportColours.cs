using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    internal interface IReportColours
    {
        Color BackgroundColour { get; }

        Color ComboBoxBorderColour { get; }

        Color ComboBoxColour { get; }

        Color ComboBoxTextColour { get; }

        Color CoverageTableActiveSortColour { get; }

        Color CoverageTableExpandCollapseIconColour { get; }

        Color CoverageTableHeaderFontColour { get; }

        Color CoverageTableInactiveSortColour { get; }

        Color CoverageTableRowHoverBackgroundColour { get; }

        Color DivHeaderBackgroundColour { get; }

        Color FontColour { get; }

        Color GrayCoverage { get; }

        Color HeaderBorderColour { get; }

        Color HeaderFontColour { get; }

        Color LinkColour { get; }

        Color ScrollBarArrowColour { get; }

        Color ScrollBarThumbColour { get; }

        Color ScrollBarTrackColour { get; }

        Color TabBackgroundColour { get; }

        Color TableBorderColour { get; }

        Color TextBoxBorderColour { get; }

        Color TextBoxColour { get; }

        Color TextBoxTextColour { get; }
    }

    internal class ReportColours : IReportColours
    {
        public Color BackgroundColour { get; set; }

        public Color ComboBoxBorderColour { get; set; }

        public Color ComboBoxColour { get; set; }

        public Color ComboBoxTextColour { get; set; }

        public Color CoverageTableActiveSortColour { get; set; }

        public Color CoverageTableExpandCollapseIconColour { get; set; }

        public Color CoverageTableHeaderFontColour { get; set; }

        public Color CoverageTableInactiveSortColour { get; set; }

        public Color CoverageTableRowHoverBackgroundColour { get; set; }

        public Color DivHeaderBackgroundColour { get; set; }

        public Color FontColour { get; set; }

        public Color GrayCoverage { get; set; }

        public Color HeaderBorderColour { get; set; }

        public Color HeaderFontColour { get; set; }

        public Color LinkColour { get; set; }

        public Color ScrollBarArrowColour { get; set; }

        public Color ScrollBarThumbColour { get; set; }

        public Color ScrollBarTrackColour { get; set; }

        public Color TabBackgroundColour { get; set; }

        public Color TableBorderColour { get; set; }

        public Color TextBoxBorderColour { get; set; }

        public Color TextBoxColour { get; set; }

        public Color TextBoxTextColour { get; set; }
    }
}
