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

}
