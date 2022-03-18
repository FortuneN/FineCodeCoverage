using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Collections.Generic;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface IRunSettingsTemplateReplacementsFactory
    {
        IRunSettingsTemplateReplacements Create(
            IEnumerable<ITestContainer> testContainers,
            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup,
            string testAdapter
        );

        IRunSettingsTemplateReplacements Create(
                IMsCodeCoverageIncludesExcludesOptions coverageProjectSettings,
                string resultsDirectory,
                string enabled,
                IEnumerable<string> modulePathsInclude,
                IEnumerable<string> modulePathsExclude,
                string testAdapter
            );
    }
}
