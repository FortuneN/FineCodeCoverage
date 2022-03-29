using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{

    internal interface IProjectRunSettingsGenerator
    {
        Task RemoveGeneratedProjectSettingsAsync(IEnumerable<ICoverageProject> coverageProjects);
        Task WriteProjectsRunSettingsAsync(IEnumerable<ICoverageProjectRunSettings> coverageProjectsRunSettings);
    }

}
