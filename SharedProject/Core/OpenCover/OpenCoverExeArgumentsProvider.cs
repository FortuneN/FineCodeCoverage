using FineCodeCoverage.Engine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal static class CommandLineArguments
    {
        public static string AddQuotes(string value)
        {
            return $@"""{value}""";
        }
        
        public static string AddEscapeQuotes(string arg)
        {
            return $@"\""{arg}\""";
        }
    }
    [Export(typeof(IOpenCoverExeArgumentsProvider))]
    internal class OpenCoverExeArgumentsProvider : IOpenCoverExeArgumentsProvider
    {
        private enum Delimiter { Semicolon, Space}
        private void AddFilter(ICoverageProject project, List<string> opencoverSettings)
        {
            var includeFilters = GetExcludesOrIncludes(project.Settings.Include, project.IncludedReferencedProjects,true);
            var excludeFilters = GetExcludesOrIncludes(project.Settings.Exclude, project.ExcludedReferencedProjects,false);
            AddIncludeAllIfExcludingWithoutIncludes();
            var filters = includeFilters.Concat(excludeFilters).ToList();
            SafeAddToSettingsDelimitedIfAny(opencoverSettings, "filter", filters, Delimiter.Space);
            
            void AddIncludeAllIfExcludingWithoutIncludes()
            {
                if (excludeFilters.Any() && !includeFilters.Any())
                {
                    includeFilters.Add("+[*]*");
                }
            }

            List<string> GetExcludesOrIncludes(string[] excludesOrIncludes,List<string> referencedExcludesOrIncludes, bool isInclude)
            {
                var excludeOrIncludeFilters = new List<string>();
                var prefix = IncludeSymbol(isInclude);
                excludesOrIncludes = SanitizeExcludesOrIncludes(excludesOrIncludes).ToArray();
                
                foreach (var value in excludesOrIncludes)
                {
                    excludeOrIncludeFilters.Add($@"{prefix}{value}");
                }

                foreach (var includedReferencedProject in referencedExcludesOrIncludes)
                {
                    excludeOrIncludeFilters.Add(IncludeOrExclude(isInclude, includedReferencedProject));
                }
                return excludeOrIncludeFilters.Distinct().ToList();
            }

            string IncludeOrExclude(bool include,string moduleFilter,string classFilter = "*")
            {
                var filter = IncludeSymbol(include);
                return $"{filter}[{moduleFilter}]{classFilter}";
            }

            string IncludeSymbol(bool include) => include ? "+" : "-"; 
        }

        private IEnumerable<string> SanitizeExcludesOrIncludes(string[] excludesOrIncludes)
        {
            return (excludesOrIncludes ?? new string[0])
                .Where(x => x != null)
                .Select(x => x.Trim(' ', '\'', '\"'))
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        private void SafeAddToSettingsDelimitedIfAny(
            List<string> opencoverSettings,
            string settingName,
            IEnumerable<string> settings,
            Delimiter delimiter = Delimiter.Semicolon
        )
        {
            if (settings.Any())
            {
                var delimit = delimiter == Delimiter.Semicolon ? ";" : " ";
                opencoverSettings.Add($@"""-{settingName}:{string.Join(delimit, settings)}""");
            }
        }

        private void AddExcludeByFile(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludes = SanitizeExcludesOrIncludes(project.Settings.ExcludeByFile).ToList();
            SafeAddToSettingsDelimitedIfAny(opencoverSettings, "excludebyfile", excludes);
        }

        private void AddExcludeByAttribute(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludeFromCodeCoverageAttributes = new List<string>()
                {
					// coverlet knows these implicitly
					"ExcludeFromCoverage",
                    "ExcludeFromCodeCoverage"
                };

            var excludes = SanitizeExcludesOrIncludes(project.Settings.ExcludeByAttribute)
                .Concat(excludeFromCodeCoverageAttributes)
                .SelectMany(exclude => new[] { exclude, GetAlternateName(exclude) })
                .OrderBy(exclude => exclude)
                .Select(WildCardIfShortName);
                
            
            SafeAddToSettingsDelimitedIfAny(opencoverSettings, "excludebyattribute", excludes);
            
            string WildCardIfShortName(string exclude)
            {
                if(exclude.IndexOf(".") == -1)
                {
                    return $"*.{exclude}";
                }
                return exclude;
            }

            string GetAlternateName(string exclude)
            {
                if (exclude.EndsWith("Attribute"))
                {
                    // remove 'Attribute' suffix
                    return exclude.Substring(0, exclude.Length - 9);
                }
                else
                {
                    // add 'Attribute' suffix
                    return $"{exclude}Attribute";
                }
            }

        }

        private string GetTargetArgs(ICoverageProject project)
        {
            var runSettings = !string.IsNullOrWhiteSpace(project.RunSettingsFile) ? $@" /Settings:{CommandLineArguments.AddEscapeQuotes(project.RunSettingsFile)}" : default;
            return $@"""-targetargs:{CommandLineArguments.AddEscapeQuotes(project.TestDllFile)}{runSettings}""";
        }

        public List<string> Provide(ICoverageProject project,string msTestPlatformExePath)
        {
            var opencoverSettings = new List<string>();
            
            opencoverSettings.Add(CommandLineArguments.AddQuotes($"-target:{msTestPlatformExePath}"));
            opencoverSettings.Add(GetTargetArgs(project));

            opencoverSettings.Add(CommandLineArguments.AddQuotes($"-output:{project.CoverageOutputFile}"));
            
            AddFilter(project, opencoverSettings);
            AddExcludeByFile(project, opencoverSettings);
            AddExcludeByAttribute(project, opencoverSettings);
            opencoverSettings.Add($"-register:path{(project.Is64Bit ? "64" : "32")}");
            opencoverSettings.Add("-mergebyhash");
            opencoverSettings.Add("-hideskipped:all");

            return opencoverSettings;
            
        }
    }
}
