using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Output
{
    internal class OutputToolWindowContext
    {
		public ScriptManager ScriptManager { get; set; }
		public IFCCEngine FccEngine { get; set; }
	}
}
