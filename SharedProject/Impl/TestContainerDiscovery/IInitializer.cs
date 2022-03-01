using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    internal interface IInitializer : IInitializeStatusProvider
    {
        Task InitializeAsync();
    }

}

