using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace FineCodeCoverage.Engine.ReportGenerator
{
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
