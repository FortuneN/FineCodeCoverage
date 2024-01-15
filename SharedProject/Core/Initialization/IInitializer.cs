using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Initialization
{
    internal interface IInitializer : IInitializeStatusProvider
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }

}

