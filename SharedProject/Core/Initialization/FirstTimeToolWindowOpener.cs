using FineCodeCoverage.Core.Utilities;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Initialization
{
    [Export(typeof(IFirstTimeToolWindowOpener))]
    internal class FirstTimeToolWindowOpener : IFirstTimeToolWindowOpener
    {
        private readonly IInitializedFromTestContainerDiscoverer initializedFromTestContainerDiscoverer;
        private readonly IShownToolWindowHistory shownToolWindowHistory;
        private readonly IToolWindowOpener toolWindowOpener;

        [ImportingConstructor]
        public FirstTimeToolWindowOpener(
            IInitializedFromTestContainerDiscoverer initializedFromTestContainerDiscoverer,
            IShownToolWindowHistory shownToolWindowHistory,
            IToolWindowOpener toolWindowOpener
        )
        {
            this.initializedFromTestContainerDiscoverer = initializedFromTestContainerDiscoverer;
            this.shownToolWindowHistory = shownToolWindowHistory;
            this.toolWindowOpener = toolWindowOpener;
        }

        public async Task OpenIfFirstTimeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                initializedFromTestContainerDiscoverer.InitializedFromTestContainerDiscoverer &&
                !shownToolWindowHistory.HasShownToolWindow
            )
            {
                await toolWindowOpener.OpenToolWindowAsync();
            }
        }
    }
}
