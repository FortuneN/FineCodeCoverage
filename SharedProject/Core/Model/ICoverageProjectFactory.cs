using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.Model
{
    internal interface ICoverageProjectFactory
    {
		Task<ICoverageProject> CreateAsync();
        void Initialize();
    }
}
