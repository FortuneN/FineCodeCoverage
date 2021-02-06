using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using FineCodeCoverage.Engine.Cobertura;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Engine.Utilities;
using FineCodeCoverage.Options;
using Microsoft;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine
{
    internal static class FCCEngine
	{
		private static object colorThemeService;
		private static string CurrentTheme => $"{((dynamic)colorThemeService)?.CurrentTheme?.Name}".Trim();
		private static DTE dte;
		private static CancellationTokenSource cancellationTokenSource;
		private static CancellationToken cancellationToken;
		public static event UpdateMarginTagsDelegate UpdateMarginTags;
		public static event UpdateOutputWindowDelegate UpdateOutputWindow;
		
		public static string HtmlFilePath { get; private set; }
		public static string AppDataFolder { get; private set; }
		public static CoverageReport CoverageReport { get; private set; }
		public static List<CoverageLine> CoverageLines { get; private set; } = new List<CoverageLine>();
		
		public static void Initialize(IServiceProvider serviceProvider)
		{
			colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				dte = (DTE)serviceProvider.GetService(typeof(DTE));
				Assumes.Present(dte);
			});

			CreateAppDataFolder();
			
			CleanupLegacyFolders();

			CoverletUtil.Initialize(AppDataFolder);
			ReportGeneratorUtil.Initialize(AppDataFolder);
			MsTestPlatformUtil.Initialize(AppDataFolder);
			OpenCoverUtil.Initialize(AppDataFolder);
		}

		private static void CreateAppDataFolder()
        {
			AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Vsix.Code);
			Directory.CreateDirectory(AppDataFolder);
		}

		private static void CleanupLegacyFolders()
		{
			Directory
			.GetDirectories(AppDataFolder, "*", SearchOption.TopDirectoryOnly)
			.Where(path =>
			{
				var name = Path.GetFileName(path);

				if (name.Contains("__"))
				{
					return true;
				}

				if (Guid.TryParse(name, out var _))
				{
					return true;
				}

				return false;
			})
			.ToList()
			.ForEach(path =>
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch
				{
					// ignore
				}
			});
		}

		public static IEnumerable<CoverageLine> GetLines(string filePath, int startLineNumber, int endLineNumber)
		{
			return CoverageLines
			.AsParallel()
			.Where(x => x.Class.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase))
			.Where(x => x.Line.Number >= startLineNumber && x.Line.Number <= endLineNumber)
			.ToArray();
		}

		public static string[] GetSourceFiles(string assemblyName, string qualifiedClassName)
		{
			// Note : There may be more than one file; e.g. in the case of partial classes

			var package = CoverageReport
				.Packages.Package
				.SingleOrDefault(x => x.Name.Equals(assemblyName));

			if (package == null)
			{
				return new string[0];
			}

			var classFiles = package
				.Classes.Class
				.Where(x => x.Name.Equals(qualifiedClassName))
				.Select(x => x.Filename)
				.ToArray();

			return classFiles;
		}
		
		public static void StopCoverage()
		{
			if(cancellationTokenSource != null)
            {
				cancellationTokenSource.Cancel();
			}
		}
		
		public static bool CanRunCoverage()
        {
			var canRun = AppOptions.Get().Enabled;

			if (!canRun)
			{
				CoverageLines.Clear();
				UpdateMarginTags?.Invoke(null);
				UpdateOutputWindow?.Invoke(null);
				
			}
			return canRun;
		}
		
		private static void SetCancellationToken()
        {
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			ProcessUtil.CancellationToken = cancellationToken;
		}
		private static void Reset()
        {
			StopCoverage();
			SetCancellationToken();
			HtmlFilePath = null;
			CoverageLines.Clear();
		}

		private static async System.Threading.Tasks.Task RunCoverToolAsync(CoverageProject project)
		{
			if (project.IsDotNetSdkStyle())
			{
				await CoverletUtil.RunCoverletAsync(project, true);
			}
			else
			{
				await OpenCoverUtil.RunOpenCoverAsync(project, true);
			}
		}

		private static async System.Threading.Tasks.Task PrepareCoverageProjectsAsync(List<CoverageProject> coverageProjects)
        {
			foreach (var project in coverageProjects)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (string.IsNullOrWhiteSpace(project.ProjectFile))
				{
					project.FailureDescription = $"Unsupported project type for DLL '{project.TestDllFile}'";
					continue;
				}

				if (!project.Settings.Enabled)
				{
					project.FailureDescription = $"Disabled";
					continue;
				}

				await project.PrepareForCoverageAsync(dte);
			}
		}

		public static void ReloadCoverage(List<CoverageProject> projects)
		{
			Logger.Log("================================== START ==================================");
			
			Reset();
			
			var coverageTask = System.Threading.Tasks.Task.Run(async () =>
              {

				// process pipeline

				await PrepareCoverageProjectsAsync(projects);

				foreach (var project in projects)
				{
					cancellationToken.ThrowIfCancellationRequested();
					await project.StepAsync("Run Coverage Tool", RunCoverToolAsync);
				}

				var passedProjects = projects.Where(x => !x.HasFailed);

				var coverOutputFiles = passedProjects
						.Select(x => x.CoverageOutputFile)
						.ToArray();

				if (coverOutputFiles.Any())
				{
					cancellationToken.ThrowIfCancellationRequested();
					  // run reportGenerator process

					var darkMode = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);

					var result = await ReportGeneratorUtil.RunReportGeneratorAsync(coverOutputFiles, darkMode, true);

					if (result.Success)
					{
						// update CoverageLines

						CoverageReport = CoberturaUtil.ProcessCoberturaXmlFile(result.UnifiedXmlFile, out var coverageLines);
						  CoverageLines = coverageLines;
						// update HtmlFilePath

						ReportGeneratorUtil.ProcessUnifiedHtmlFile(result.UnifiedHtmlFile, darkMode, out var htmlFilePath);
						  HtmlFilePath = htmlFilePath;
					}

					// update margins

					{
						UpdateMarginTagsEventArgs updateMarginTagsEventArgs = null;

						try
						{
							updateMarginTagsEventArgs = new UpdateMarginTagsEventArgs
							{
							};
						}
						catch
						{
							// ignore
						}
						finally
						{
							UpdateMarginTags?.Invoke(updateMarginTagsEventArgs);
						}
					}

					// update output window

					{
						UpdateOutputWindowEventArgs updateOutputWindowEventArgs = null;

						try
						{
							if (!string.IsNullOrEmpty(HtmlFilePath))
							{
								updateOutputWindowEventArgs = new UpdateOutputWindowEventArgs
								{
									HtmlContent = File.ReadAllText(HtmlFilePath)
								};
							}
						}
						catch
						{
							// ignore
						}
						finally
						{
							UpdateOutputWindow?.Invoke(updateOutputWindowEventArgs);
						}
					}

				}


				// log

				Logger.Log("================================== DONE ===================================");

				cancellationTokenSource.Dispose();
				cancellationTokenSource = null;
			  },cancellationToken);
			
		}
		
	}
	
}