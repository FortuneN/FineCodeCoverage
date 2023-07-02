using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IRunSettingsTemplateReplacementsFactory))]
    internal class RunSettingsTemplateReplacementsFactory : IRunSettingsTemplateReplacementsFactory
    {
        private class RunSettingsTemplateReplacements : IRunSettingsTemplateReplacements
        {
            public string Enabled { get; set; }
            public string ResultsDirectory { get; set; }
            public string TestAdapter { get; set; }
            public string ModulePathsExclude { get; set; }
            public string ModulePathsInclude { get; set; }
            public string FunctionsExclude { get; set; }
            public string FunctionsInclude { get; set; }
            public string AttributesExclude { get; set; }
            public string AttributesInclude { get; set; }
            public string SourcesExclude { get; set; }
            public string SourcesInclude { get; set; }
            public string CompanyNamesExclude { get; set; }
            public string CompanyNamesInclude { get; set; }
            public string PublicKeyTokensExclude { get; set; }
            public string PublicKeyTokensInclude { get; set; }

            public RunSettingsTemplateReplacements(
                IMsCodeCoverageIncludesExcludesOptions settings,
                string resultsDirectory,
                string enabled,
                string testAdapter
            )
            {
                ResultsDirectory = resultsDirectory;
                TestAdapter = testAdapter;
                Enabled = enabled;
                ModulePathsExclude = GetExcludeIncludeElementsString(settings.ModulePathsExclude, "ModulePath");
                ModulePathsInclude = GetExcludeIncludeElementsString(settings.ModulePathsInclude, "ModulePath");
                FunctionsExclude = GetExcludeIncludeElementsString(settings.FunctionsExclude, "Function");
                FunctionsInclude = GetExcludeIncludeElementsString(settings.FunctionsInclude, "Function");
                AttributesExclude = GetExcludeIncludeElementsString(settings.AttributesExclude, "Attribute");
                AttributesInclude = GetExcludeIncludeElementsString(settings.AttributesInclude, "Attribute");
                SourcesExclude = GetExcludeIncludeElementsString(settings.SourcesExclude, "Source");
                SourcesInclude = GetExcludeIncludeElementsString(settings.SourcesInclude, "Source");
                CompanyNamesExclude = GetExcludeIncludeElementsString(settings.CompanyNamesExclude, "CompanyName");
                CompanyNamesInclude = GetExcludeIncludeElementsString(settings.CompanyNamesInclude, "CompanyName");
                PublicKeyTokensExclude = GetExcludeIncludeElementsString(settings.PublicKeyTokensExclude, "PublicKeyToken");
                PublicKeyTokensInclude = GetExcludeIncludeElementsString(settings.PublicKeyTokensInclude, "PublicKeyToken");
            }

            private static string GetExcludeIncludeElementsString(IEnumerable<string> excludeIncludes, string elementName)
            {
                if (excludeIncludes == null)
                {
                    return string.Empty;
                }

                var elements = excludeIncludes.Select(excludeInclude => $"<{elementName}>{excludeInclude}</{elementName}>").Distinct();
                return string.Join("", elements);
            }
        }

        private class MergedIncludesExcludesOptions : IMsCodeCoverageIncludesExcludesOptions
        {
            private readonly List<IMsCodeCoverageIncludesExcludesOptions> allOptions;
            public MergedIncludesExcludesOptions(IEnumerable<IMsCodeCoverageIncludesExcludesOptions> allOptions)
            {
                this.allOptions = allOptions.ToList();

                ModulePathsExclude = Merge(options => options.ModulePathsExclude);
                ModulePathsInclude = Merge(options => options.ModulePathsInclude);
                CompanyNamesExclude = Merge(options => options.CompanyNamesExclude);
                CompanyNamesInclude = Merge(options => options.CompanyNamesInclude);
                PublicKeyTokensExclude = Merge(options => options.PublicKeyTokensExclude);
                PublicKeyTokensInclude = Merge(options => options.PublicKeyTokensInclude);
                SourcesExclude = Merge(options => options.SourcesExclude);
                SourcesInclude = Merge(options => options.SourcesInclude);
                AttributesExclude = Merge(options => options.AttributesExclude);
                AttributesInclude = Merge(options => options.AttributesInclude);
                FunctionsExclude = Merge(options => options.FunctionsExclude);
                FunctionsInclude = Merge(options => options.FunctionsInclude);
            }

            private string[] Merge(Func<IMsCodeCoverageIncludesExcludesOptions, string[]> selector)
            {
                return allOptions.SelectMany(options => selector(options) ?? Array.Empty<string>()).ToArray();
            }

            public string[] ModulePathsExclude { get; set; }
            public string[] ModulePathsInclude { get; set; }
            public string[] CompanyNamesExclude { get; set; }
            public string[] CompanyNamesInclude { get; set; }
            public string[] PublicKeyTokensExclude { get; set; }
            public string[] PublicKeyTokensInclude { get; set; }
            public string[] SourcesExclude { get; set; }
            public string[] SourcesInclude { get; set; }
            public string[] AttributesExclude { get; set; }
            public string[] AttributesInclude { get; set; }
            public string[] FunctionsInclude { get; set; }
            public string[] FunctionsExclude { get; set; }
        }

        private class CombinedIncludesExcludesOptions : IMsCodeCoverageIncludesExcludesOptions
        {
            public CombinedIncludesExcludesOptions(IMsCodeCoverageIncludesExcludesOptions includesExcludesOptions, IEnumerable<string> additionalModulePathsIncludes, IEnumerable<string> additionalModulePathsExcludes)
            {
                CompanyNamesInclude = includesExcludesOptions.CompanyNamesInclude;
                CompanyNamesExclude = includesExcludesOptions.CompanyNamesExclude;
                PublicKeyTokensInclude = includesExcludesOptions.PublicKeyTokensInclude;
                PublicKeyTokensExclude = includesExcludesOptions.PublicKeyTokensExclude;
                SourcesExclude = includesExcludesOptions.SourcesExclude;
                SourcesInclude = includesExcludesOptions.SourcesInclude;
                AttributesExclude = includesExcludesOptions.AttributesExclude;
                AttributesInclude = includesExcludesOptions.AttributesInclude;
                FunctionsInclude = includesExcludesOptions.FunctionsInclude;
                FunctionsExclude = includesExcludesOptions.FunctionsExclude;
                var modulePathsIncludesFromOptions = includesExcludesOptions.ModulePathsInclude ?? Enumerable.Empty<string>();
                var modulePathsExcludesFromOptions = includesExcludesOptions.ModulePathsExclude ?? Enumerable.Empty<string>();
                ModulePathsInclude = additionalModulePathsIncludes.Concat(modulePathsIncludesFromOptions).ToArray();
                ModulePathsExclude = additionalModulePathsExcludes.Concat(modulePathsExcludesFromOptions).ToArray();
            }
            public string[] ModulePathsExclude { get; set; }
            public string[] ModulePathsInclude { get; set; }
            public string[] CompanyNamesExclude { get; set; }
            public string[] CompanyNamesInclude { get; set; }
            public string[] PublicKeyTokensExclude { get; set; }
            public string[] PublicKeyTokensInclude { get; set; }
            public string[] SourcesExclude { get; set; }
            public string[] SourcesInclude { get; set; }
            public string[] AttributesExclude { get; set; }
            public string[] AttributesInclude { get; set; }
            public string[] FunctionsInclude { get; set; }
            public string[] FunctionsExclude { get; set; }
        }

        public IRunSettingsTemplateReplacements Create(
            IEnumerable<ITestContainer> testContainers,
            Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup,
            string testAdapter)
        {
            var allProjectDetails = testContainers.Select(tc => userRunSettingsProjectDetailsLookup[tc.Source]).ToList();
            var resultsDirectory = allProjectDetails[0].CoverageOutputFolder;
            var allSettings = allProjectDetails.Select(pd => pd.Settings);
            var allProjectsDisabled = allSettings.All(s => !s.Enabled);
            var mergedSettings = new MergedIncludesExcludesOptions(allSettings);


            var additionalModulePathsExclude = allProjectDetails.SelectMany(pd =>
            {
                var additional = pd.ExcludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)).ToList();
                if (!pd.Settings.IncludeTestAssembly)
                {
                    additional.Add(MsCodeCoverageRegex.RegexEscapePath(pd.TestDllFile));
                }
                return additional;

            });

            var additionalModulePathsInclude = allProjectDetails.SelectMany(projectDetails => projectDetails.IncludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)));
            var settings = new CombinedIncludesExcludesOptions(mergedSettings, additionalModulePathsInclude, additionalModulePathsExclude);
            return new RunSettingsTemplateReplacements(settings, resultsDirectory, (!allProjectsDisabled).ToString().ToLower(), testAdapter);
        }

        public IRunSettingsTemplateReplacements Create(ICoverageProject coverageProject, string testAdapter)
        {
            var projectSettings = coverageProject.Settings;
            var additionalModulePathsExclude = coverageProject.ExcludedReferencedProjects.Select(
                rp => MsCodeCoverageRegex.RegexModuleName(rp)).ToList();

            if (!projectSettings.IncludeTestAssembly)
            {
                additionalModulePathsExclude.Add(MsCodeCoverageRegex.RegexEscapePath(coverageProject.TestDllFile));
            }

            var additionalModulePathsInclude = coverageProject.IncludedReferencedProjects.Select(rp => MsCodeCoverageRegex.RegexModuleName(rp)).ToList();
            var settings = new CombinedIncludesExcludesOptions(projectSettings, additionalModulePathsInclude, additionalModulePathsExclude);
            return new RunSettingsTemplateReplacements(settings, coverageProject.CoverageOutputFolder, projectSettings.Enabled.ToString(), testAdapter);
        }
    }

}
