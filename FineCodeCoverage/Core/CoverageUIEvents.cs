using System;

namespace FineCodeCoverage.Engine
{
    internal class UpdateMarginTagsEventArgs : EventArgs
    {
    }

    internal class UpdateOutputWindowEventArgs : EventArgs
    {
        public string HtmlContent { get; set; }
    }

    internal delegate void UpdateMarginTagsDelegate(UpdateMarginTagsEventArgs e);
    internal delegate void UpdateOutputWindowDelegate(UpdateOutputWindowEventArgs e);
}
