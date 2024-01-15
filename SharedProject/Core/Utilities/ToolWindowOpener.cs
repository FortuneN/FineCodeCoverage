using FineCodeCoverage.Output;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(IToolWindowOpener))]
    internal class ToolWindowOpener : IToolWindowOpener
    {
        public async Task OpenToolWindowAsync()
        {
            await OutputToolWindowCommand.Instance.ShowToolWindowAsync();
        }
    }
}
