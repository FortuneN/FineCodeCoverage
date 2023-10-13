using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Output
{
    internal class OutputToolWindowContext
    {
		public IEventAggregator EventAggregator { get; set; }
        public bool ShowToolbar { get; set; }
	}
}
