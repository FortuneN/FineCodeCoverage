using FineCodeCoverage.Core.Cobertura;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.MsTestPlatform;
using FineCodeCoverage.Core.OpenCover;
using FineCodeCoverage.Core.ReportGenerator;
using FineCodeCoverage.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Coverage
{
	public class CoverageService : ICoverageService
	{
		private readonly ILogger _logger;
		private readonly ServerSettings _serverSettings;
		private readonly ICoverletService _coverletService;
		private readonly ICoberturaService _coberturaService;
		private readonly IOpenCoverService _openCoverService;
		private readonly IMsTestPlatformService _msTestPlatformService;
		private readonly IReportGeneratorService _reportGeneratorService;

		public CoverageService
		(
			ServerSettings serverSettings,
			ILogger<CoverageService> logger,
			ICoverletService coverletService,
			ICoberturaService coberturaService,
			IOpenCoverService openCoverService,
			IMsTestPlatformService msTestPlatformService,
			IReportGeneratorService reportGeneratorService
		)
		{
			_logger = logger;
			_serverSettings = serverSettings;
			_coverletService = coverletService;
			_coberturaService = coberturaService;
			_openCoverService = openCoverService;
			_msTestPlatformService = msTestPlatformService;
			_reportGeneratorService = reportGeneratorService;
		}

		public async Task InitializeAsync()
		{
			CleanupLegacyFolders();

			_coverletService.Initialize();
			_reportGeneratorService.Initialize();
			await _openCoverService.InitializeAsync();
			await _msTestPlatformService.InitializeAsync();
		}

		private void CleanupLegacyFolders()
		{
			Directory
			.GetDirectories(_serverSettings.AppDataFolder, "*", SearchOption.TopDirectoryOnly)
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

		public void ClearProcesses()
		{
			ProcessUtil.ClearProcesses();
		}

		public async Task<CalculateCoverageResponse> CalculateCoverageAsync(CalculateCoverageRequest request)
		{
			var response = new CalculateCoverageResponse { RequestId = request.RequestId };

			// reset

			ClearProcesses();

			// process pipeline

			if (request.Projects == null)
			{
				request.Projects = new CoverageProject[0];
			}

			//Parallel.ForEach(request.Projects, async project =>
			//{
			//	try
			//	{
			//		await CalculateCoverageAsync(project, request.Settings);
			//	}
			//	catch (Exception exception)
			//	{
			//		project.Error = exception.ToString();
			//		await project.Logger.LogErrorAsync(exception);
			//	}
			//});

			foreach (var project in request.Projects)
			{
				try
				{
					project.RequestId = request.RequestId;
					await CalculateCoverageAsync(project, request.Settings);
				}
				catch (Exception exception)
				{
					project.Error = exception.ToString();
					
					await _systemLogger.LogAsync("PROJECT_ERROR", new
					{
						Message = exception.Message,
						ToString = exception.ToString()
					});
				}
			}

			// filter for successful cover output files

			var coverOutputFiles = request.Projects
				.Where(x => x.Settings.Enabled)
				.Where(x => !string.IsNullOrWhiteSpace(x.Error))
				.Select(x => x.CoverageOutputFile)
				.ToArray();
				
			if (!coverOutputFiles.Any())
			{
				return response;
			}

			// run reportGenerator process

			var reportGeneratorResult = await _reportGeneratorService.RunAsync(coverOutputFiles, request.DarkMode);

			// update CoverageLines

			response.CoverageReport = _coberturaService.ProcessCoberturaXmlFile(reportGeneratorResult.UnifiedXmlFile, out var coverageLines);
			response.CoverageLines = coverageLines;

			// update HtmlFilePath

			var coverageHtmlFile = await _reportGeneratorService.ProcessUnifiedHtmlFileAsync(reportGeneratorResult.UnifiedHtmlFile, request.DarkMode);
			response.HtmlContent = await File.ReadAllTextAsync(coverageHtmlFile);

			// return

			return response;
		}

		public async Task CalculateCoverageAsync(CoverageProject project, CoverageProjectSettings defaultSettings)
		{
			using var msbWorkspace = MSBuildWorkspace.Create();
			var msbProject = await msbWorkspace.OpenProjectAsync(project.ProjectFile);

			project.ProjectName = msbProject.Name;
			project.ProjectFileXElement = await XElementUtil.LoadAsync(project.ProjectFile, true);

			CoverageProjectUtil.ApplySettings(project, defaultSettings);

			if (!project.Settings.Enabled)
			{
				return;
			}

			project.Is64Bit = msbProject.CompilationOptions.Platform == Platform.X64;
			project.CoverageOutputFolder = Path.Combine(project.ProjectOutputFolder, "fine-code-coverage");
			project.CoverageOutputFile = Path.Combine(project.CoverageOutputFolder, "project.coverage.xml");
			project.IsDotNetSdkStyle = CoverageProjectUtil.IsDotNetSdkStyle(project);
			project.ReferencedProjects = await CoverageProjectUtil.GetReferencedProjectsAsync(project); //TODO:msbProject.ProjectReferences
			project.HasExcludeFromCodeCoverageAssemblyAttribute = CoverageProjectUtil.HasExcludeFromCodeCoverageAssemblyAttribute(project.ProjectFileXElement);
			project.AssemblyName = string.IsNullOrWhiteSpace(msbProject.AssemblyName) ? CoverageProjectUtil.GetAssemblyName(project.ProjectFileXElement, Path.GetFileNameWithoutExtension(project.ProjectFile)) : msbProject.AssemblyName;

			// cleanup

			if (!Directory.Exists(project.CoverageOutputFolder))
			{
				Directory.CreateDirectory(project.CoverageOutputFolder);
			}

			try
			{
				var legacyOutputFolder = Path.Combine(project.ProjectOutputFolder, "_outputFolder");
				Directory.Delete(legacyOutputFolder, true);
			}
			catch
			{
				// ignore
			}

			try
			{
				var defaultOutputFolder = Path.GetDirectoryName(project.ProjectOutputFolder);
				var legacyWorkFolder = Path.Combine(defaultOutputFolder, "fine-code-coverage");
				Directory.Delete(legacyWorkFolder, true);
			}
			catch
			{
				// ignore
			}

			//  run cover tool

			if (project.IsDotNetSdkStyle)
			{
				await _coverletService.RunCoverletAsync(project);
			}
			else
			{
				await _openCoverService.RunOpenCoverAsync(project);
			}
		}
	}
}
