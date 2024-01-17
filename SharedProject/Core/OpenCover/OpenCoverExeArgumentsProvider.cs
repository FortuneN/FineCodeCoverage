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

        private void AddExcludeByFile(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludes = (project.Settings.ExcludeByFile ?? new string[0])
                .Where(x => x != null)
                .Select(x => x.Trim(' ', '\'', '\"'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (excludes.Any())
            {
                opencoverSettings.Add($@"""-excludebyfile:{string.Join(";", excludes)}""");
            }
        }

        private void AddExcludeByAttribute(ICoverageProject project, List<string> opencoverSettings)
        {
            var excludes = new List<string>()
                {
					// coverlet knows these implicitly
					"ExcludeFromCoverage",
                    "ExcludeFromCodeCoverage"
                };

            foreach (var value in (project.Settings.ExcludeByAttribute ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                excludes.Add(value.Replace("\"", "\\\"").Trim(' ', '\''));
            }

            foreach (var exclude in excludes.ToArray())
            {
                var excludeAlternateName = default(string);

                if (exclude.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
                {
                    // remove 'Attribute' suffix
                    excludeAlternateName = exclude.Substring(0, exclude.IndexOf("Attribute", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    // add 'Attribute' suffix
                    excludeAlternateName = $"{exclude}Attribute";
                }

                excludes.Add(excludeAlternateName);
            }

            excludes = excludes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

            if (excludes.Any())
            {
                opencoverSettings.Add($@" ""-excludebyattribute:(*.{string.Join(")|(*.", excludes)})"" ");
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
