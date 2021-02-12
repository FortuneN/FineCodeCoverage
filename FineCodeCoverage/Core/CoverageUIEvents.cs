using System;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine
{
    internal class UpdateMarginTagsEventArgs : EventArgs { 
		public List<CoverageLine> CoverageLines { get; set; }
	}

	internal class UpdateOutputWindowEventArgs : EventArgs
	{
		public string HtmlContent { get; set; }
	}

	internal delegate void UpdateMarginTagsDelegate(UpdateMarginTagsEventArgs e);
	internal delegate void UpdateOutputWindowDelegate(UpdateOutputWindowEventArgs e);
}
