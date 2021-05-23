using System.Threading.Tasks;
using System.Threading;

namespace FineCodeCoverage.Core.Utilities
{
    interface IProcessUtil
    {
        Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request);
        CancellationToken CancellationToken { get; set; }

    }
}
