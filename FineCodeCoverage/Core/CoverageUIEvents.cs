using System;

namespace FineCodeCoverage.Engine
{
	public class UpdateMarginTagsEventArgs : EventArgs
	{
	}

	public class UpdateOutputWindowEventArgs : EventArgs
	{
		public string HtmlContent { get; set; }
	}

	public delegate void UpdateMarginTagsDelegate(UpdateMarginTagsEventArgs e);
	public delegate void UpdateOutputWindowDelegate(UpdateOutputWindowEventArgs e);
}
