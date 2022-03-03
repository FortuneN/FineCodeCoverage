using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    internal interface IPackageInitializer
    {
        Task InitializeAsync(System.Threading.CancellationToken cancellationToken);
    }

}

