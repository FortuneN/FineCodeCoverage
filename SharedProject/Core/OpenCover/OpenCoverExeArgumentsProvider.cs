using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
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
            var includedModules = project.IncludedReferencedProjects.ToList();
            if(project.Settings.IncludeTestAssembly)
            {
                includedModules.Add(project.ProjectName);
            }
            var includeFilters = GetExcludesOrIncludes(project.Settings.Include, includedModules, true);
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

            List<string> GetExcludesOrIncludes(
                IEnumerable<string> excludesOrIncludes,IEnumerable<string> moduleExcludesOrIncludes, bool isInclude)
            {
                var excludeOrIncludeFilters = new List<string>();
                var prefix = IncludeSymbol(isInclude);
                var sanitizedExcludesOrIncludes = SanitizeExcludesOrIncludes(excludesOrIncludes);
                
                foreach (var value in sanitizedExcludesOrIncludes)
                {
                    excludeOrIncludeFilters.Add($@"{prefix}{value}");
                }

                foreach (var moduleExcludeOrInclude in moduleExcludesOrIncludes)
                {
                    excludeOrIncludeFilters.Add(IncludeOrExcludeModule(isInclude, moduleExcludeOrInclude));
                }
                return excludeOrIncludeFilters.Distinct().ToList();
            }

            string IncludeOrExcludeModule(bool include,string moduleFilter,string classFilter = "*")
            {
                var filter = IncludeSymbol(include);
                return $"{filter}[{moduleFilter}]{classFilter}";
            }

            string IncludeSymbol(bool include) => include ? "+" : "-"; 
        }

        private IEnumerable<string> SanitizeExcludesOrIncludes(IEnumerable<string> excludesOrIncludes)
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
            var openCoverTargetArgs = project.Settings.OpenCoverTargetArgs;
            var additionalTargetArgs = !string.IsNullOrWhiteSpace(openCoverTargetArgs) ? $" {openCoverTargetArgs}" : default;
            return $@"""-targetargs:{CommandLineArguments.AddEscapeQuotes(project.TestDllFile)}{runSettings}{additionalTargetArgs}""";
        }

        private void AddTargetAndTargetArgs(ICoverageProject project, List<string> opencoverSettings, string msTestPlatformExePath)
        {
            var target = !string.IsNullOrWhiteSpace(project.Settings.OpenCoverTarget) ? project.Settings.OpenCoverTarget : msTestPlatformExePath;
            opencoverSettings.Add(CommandLineArguments.AddQuotes($"-target:{target}"));
            opencoverSettings.Add(GetTargetArgs(project));
        }

        private string GetRegister(ICoverageProject project)
        {
            var openCoverRegister = project.Settings.OpenCoverRegister;
            if (openCoverRegister == OpenCoverRegister.Default)
            {
                return $":path{(project.Is64Bit ? "64" : "32")}";
            }
            if(openCoverRegister == OpenCoverRegister.NoArg)
            {
                return "";
            }
            return $":{project.Settings.OpenCoverRegister.ToString().ToLower()}";
        }

        public List<string> Provide(ICoverageProject project,string msTestPlatformExePath)
        {
            var opencoverSettings = new List<string>();
            AddTargetAndTargetArgs(project, opencoverSettings, msTestPlatformExePath);

            opencoverSettings.Add(CommandLineArguments.AddQuotes($"-output:{project.CoverageOutputFile}"));
            
            AddFilter(project, opencoverSettings);
            AddExcludeByFile(project, opencoverSettings);
            AddExcludeByAttribute(project, opencoverSettings);
            opencoverSettings.Add($"-register{GetRegister(project)}");
            opencoverSettings.Add("-mergebyhash");
            opencoverSettings.Add("-hideskipped:all");

            return opencoverSettings;
            
        }
    }
}
