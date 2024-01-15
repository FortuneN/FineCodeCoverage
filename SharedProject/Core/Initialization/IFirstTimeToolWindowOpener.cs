using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Initialization
{
    internal interface IFirstTimeToolWindowOpener
    {
        Task OpenIfFirstTimeAsync(CancellationToken cancellationToken);
    }
}
