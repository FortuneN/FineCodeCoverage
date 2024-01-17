using FineCodeCoverage.Engine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Engine.OpenCover
{
    internal static class OpenCoverExeEscaper
    {
        public static string EscapeArgument(string value)
        {
            return $@"""{value}""";
        }
        
        public static string EscapeQuoteTargetArgsArgument(string arg)
        {
            return $@"\""{arg}\""";
        }
    }
    [Export(typeof(IOpenCoverExeArgumentsProvider))]
    internal class OpenCoverExeArgumentsProvider : IOpenCoverExeArgumentsProvider
    {
        private void AddFilter(ICoverageProject project, List<string> opencoverSettings)
        {
            var filters = new List<string>();
            var defaultFilter = "+[*]*";

            foreach (var value in (project.Settings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                filters.Add($@"+{value.Replace("\"", "\\\"").Trim(' ', '\'')}");
            }

            foreach (var includedReferencedProject in project.IncludedReferencedProjects)
            {
                filters.Add($@"+[{includedReferencedProject}]*");
            }

            if (!filters.Any())
            {
                filters.Add(defaultFilter);
            }

            foreach (var value in (project.Settings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                filters.Add($@"-{value.Replace("\"", "\\\"").Trim(' ', '\'')}");
            }

            foreach (var referencedProjectExcludedFromCodeCoverage in project.ExcludedReferencedProjects)
            {
                filters.Add($@"-[{referencedProjectExcludedFromCodeCoverage}]*");
            }

            if (filters.Any(x => !x.Equals(defaultFilter)))
            {
                opencoverSettings.Add($@" ""-filter:{string.Join(" ", filters.Distinct())}"" ");
            }
            
        }

        private IEnumerable<string> GetExcludes(string[] excludes)
        {
            return (excludes ?? new string[0])
                .Where(x => x != null)
                .Select(x => x.Trim(' ', '\'', '\"'))
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        private void SafeAddToSettingsSemiColonDelimitedIfAny(List<string> opencoverSettings,string settingName,IEnumerable<string> settings)
        {
            if (settings.Any())
            {
                opencoverSettings.Add($@"""-{settingName}:{string.Join(";", settings)}""");
            }
        }

        private void AddExcludeByFile(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludes = GetExcludes(project.Settings.ExcludeByFile).ToList();
            SafeAddToSettingsSemiColonDelimitedIfAny(opencoverSettings, "excludebyfile", excludes);
        }

        private void AddExcludeByAttribute(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludeFromCodeCoverageAttributes = new List<string>()
                {
					// coverlet knows these implicitly
					"ExcludeFromCoverage",
                    "ExcludeFromCodeCoverage"
                };

            var excludes = GetExcludes(project.Settings.ExcludeByAttribute)
                .Concat(excludeFromCodeCoverageAttributes)
                .SelectMany(exclude => new[] { exclude, GetAlternateName(exclude) })
                .OrderBy(exclude => exclude)
                .Select(WildCardIfShortName);
                
            
            SafeAddToSettingsSemiColonDelimitedIfAny(opencoverSettings, "excludebyattribute", excludes);
            
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
            var runSettings = !string.IsNullOrWhiteSpace(project.RunSettingsFile) ? $@" /Settings:{OpenCoverExeEscaper.EscapeQuoteTargetArgsArgument(project.RunSettingsFile)}" : default;
            return $@"""-targetargs:{OpenCoverExeEscaper.EscapeQuoteTargetArgsArgument(project.TestDllFile)}{runSettings}""";
        }

        public List<string> Provide(ICoverageProject project,string msTestPlatformExePath)
        {
            var opencoverSettings = new List<string>();
            
            opencoverSettings.Add(OpenCoverExeEscaper.EscapeArgument($"-target:{msTestPlatformExePath}"));
            opencoverSettings.Add(GetTargetArgs(project));

            opencoverSettings.Add(OpenCoverExeEscaper.EscapeArgument($"-output:{project.CoverageOutputFile}"));
            
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
