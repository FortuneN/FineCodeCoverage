using System.Threading.Tasks;

namespace FineCodeCoverage.Impl.TestContainerDiscovery
{
    internal interface ICoverageCollectableFromTestExplorer
    {
        Task<bool> IsCollectableAsync();
    }
}
