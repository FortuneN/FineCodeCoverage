using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface ICoverletConsoleExecutor
    {
		ExecuteRequest GetRequest(ICoverageProject coverageProject,string coverletSettings);
    }
}
