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
        Color CoverageTableRowHoverColour { get; }

        Color DivHeaderBackgroundColour { get; }

        Color FontColour { get; }

        Color GrayCoverageColour { get; }

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

        Color SliderLeftColour { get; }

        Color SliderRightColour { get; }

        Color SliderThumbColour { get; }

        Color ButtonBorderColour { get; }
        Color ButtonBorderDisabledColour { get; }
        Color ButtonBorderFocusedColour { get; }
        Color ButtonBorderHoverColour { get; }
        Color ButtonBorderPressedColour { get; }
        Color ButtonColour { get; }
        Color ButtonDisabledColour { get; }
        Color ButtonFocusedColour { get; }
        Color ButtonHoverColour { get; }
        Color ButtonPressedColour { get; }
        Color ButtonTextColour { get; }
        Color ButtonDisabledTextColour { get; }
        Color ButtonFocusedTextColour { get; }
        Color ButtonHoverTextColour { get; }
        Color ButtonPressedTextColour { get; }
    }

}
