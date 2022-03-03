using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
    interface IProcessUtil
    {
		Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken);
	}
}
