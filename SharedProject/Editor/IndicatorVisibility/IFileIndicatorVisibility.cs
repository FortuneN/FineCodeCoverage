using System;
using System.Collections.Generic;
using System.Text;

namespace FineCodeCoverage.Editor.IndicatorVisibility
{
    internal interface IFileIndicatorVisibility
    {
        event EventHandler VisibilityChanged;
        bool IsVisible(string filePath);
    }
}
