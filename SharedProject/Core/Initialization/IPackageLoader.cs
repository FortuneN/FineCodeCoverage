using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Initialization
{
    internal interface IPackageLoader
    {
        Task LoadPackageAsync(System.Threading.CancellationToken cancellationToken);
    }

}

