using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    internal interface ITestOperation
    {
        long FailedTests { get; }
        long TotalTests { get; }
        System.Threading.Tasks.Task<List<CoverageProject>> GetCoverageProjectsAsync();
    }
}



