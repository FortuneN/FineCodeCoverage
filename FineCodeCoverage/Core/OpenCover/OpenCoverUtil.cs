using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.OpenCover
{
    [Export(typeof(IOpenCoverUtil))]
	internal class OpenCoverUtil:IOpenCoverUtil
	{
		private string openCoverExePath;
        private readonly IMsTestPlatformUtil msTestPlatformUtil;
        private readonly IProcessUtil processUtil;
        private readonly ILogger logger;
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
		private const string zipPrefix = "openCover";
		private const string zipDirectoryName = "openCover";

		[ImportingConstructor]
		public OpenCoverUtil(
			IMsTestPlatformUtil msTestPlatformUtil,
			IProcessUtil processUtil, 
			ILogger logger, 
			IToolFolder toolFolder, 
			IToolZipProvider toolZipProvider)
        {
            this.msTestPlatformUtil = msTestPlatformUtil;
            this.processUtil = processUtil;
            this.logger = logger;
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
        }

		public void Initialize(string appDataFolder)
		{
			var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix));
			openCoverExePath = Directory
				.GetFiles(zipDestination, "OpenCover.Console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();
		}
		
		private string GetOpenCoverExePath(string customExePath)
        {
			if(!String.IsNullOrWhiteSpace(customExePath))
            {
				return customExePath;
            }
			return openCoverExePath;
        }

		public async Task<bool> RunOpenCoverAsync(ICoverageProject project, bool throwError = false)
		{
			var title = $"OpenCover Run ({project.ProjectName})";

			var opencoverSettings = new List<string>();

			opencoverSettings.Add($@" -mergebyhash ");

			opencoverSettings.Add($@" -hideskipped:all ");

			{
				// -register:

				var registerValue = "path32";

				if (project.Is64Bit)
				{
					registerValue = "path64";
				}

				opencoverSettings.Add($@" -register:{registerValue} ");
			}

			{
				// -target:

				opencoverSettings.Add($@" ""-target:{msTestPlatformUtil.MsTestPlatformExePath}"" ");
			}

			{
				// -filter:

				var filters = new List<string>();
				var defaultFilter = "+[*]*";

				foreach (var value in (project.Settings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
				{
					filters.Add($@"+{value.Replace("\"", "\\\"").Trim(' ', '\'')}");
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

			{
				// -excludebyfile:

				var excludes = new List<string>();

				foreach (var value in (project.Settings.ExcludeByFile ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
				{
					excludes.Add(value.Replace("\"", "\\\"").Trim(' ', '\''));
				}

				if (excludes.Any())
				{
					opencoverSettings.Add($@" ""-excludebyfile:{string.Join(";", excludes)}"" ");
				}
			}

			{
				// -excludebyattribute:

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

			if (!project.Settings.IncludeTestAssembly)
			{
				// deleting the pdb of the test assembly seems to work; this is a VERY VERY shameful hack :(
				
				var testDllPdbFile = Path.Combine(project.ProjectOutputFolder, Path.GetFileNameWithoutExtension(project.TestDllFile)) + ".pdb";
				File.Delete(testDllPdbFile);

				// filtering out the test-assembly blows up the entire process and nothing gets instrumented or analysed
				
				//var nameOnlyOfDll = Path.GetFileNameWithoutExtension(project.TestDllFileInWorkFolder);
				//filters.Add($@"-[{nameOnlyOfDll}]*");
			}

			var runSettings = !string.IsNullOrWhiteSpace(project.RunSettingsFile) ? $@"/Settings:\""{project.RunSettingsFile}\""" : default;
			opencoverSettings.Add($@" ""-targetargs:\""{project.TestDllFile}\"" {runSettings}"" ");

			opencoverSettings.Add($@" ""-output:{ project.CoverageOutputFile }"" ");

			logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", opencoverSettings)}");

			var result = await processUtil
			.ExecuteAsync(new ExecuteRequest
			{
				FilePath = GetOpenCoverExePath(project.Settings.OpenCoverCustomPath),
				Arguments = string.Join(" ", opencoverSettings),
				WorkingDirectory = project.ProjectOutputFolder
			});
			
			if(result != null)
            {
				if (result.ExitCode != 0)
				{
					if (throwError)
					{
						throw new Exception(result.Output);
					}

					logger.Log($"{title} Error", result.Output);
					return false;
				}

				logger.Log(title, result.Output);
				return true;
			}
			return false;
		}
	}
}
