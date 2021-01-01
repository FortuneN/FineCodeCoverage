using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.MsTestPlatform;
using FineCodeCoverage.Core.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.OpenCover
{
	[SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
	public class OpenCoverService : IOpenCoverService
	{
		private static readonly EventId RUN_OTHER = EventIdUtil.New("OPEN_COVER_RUN_OTHER");
		private static readonly EventId RUN_START = EventIdUtil.New("OPEN_COVER_RUN_START");
		private static readonly EventId RUN_ERROR = EventIdUtil.New("OPEN_COVER_RUN_ERROR");
		private static readonly EventId RUN_SUCCESS = EventIdUtil.New("OPEN_COVER_RUN_SUCCESS");
		
		private static readonly EventId INSTALL_OTHER = EventIdUtil.New("OPEN_COVER_INSTALL_OTHER");
		private static readonly EventId INSTALL_START = EventIdUtil.New("OPEN_COVER_INSTALL_START");
		private static readonly EventId INSTALL_ERROR = EventIdUtil.New("OPEN_COVER_INSTALL_ERROR");
		private static readonly EventId INSTALL_SUCCESS = EventIdUtil.New("OPEN_COVER_INSTALL_SUCCESS");

		private static readonly EventId INITIALIZE_OTHER = EventIdUtil.New("OPEN_COVER_INITIALIZE_OTHER");
		private static readonly EventId INITIALIZE_START = EventIdUtil.New("OPEN_COVER_INITIALIZE_START");
		private static readonly EventId INITIALIZE_ERROR = EventIdUtil.New("OPEN_COVER_INITIALIZE_ERROR");
		private static readonly EventId INITIALIZE_SUCCESS = EventIdUtil.New("OPEN_COVER_INITIALIZE_SUCCESS");

		private static readonly EventId GET_VERSION_OTHER = EventIdUtil.New("OPEN_COVER_GET_VERSION_OTHER");
		private static readonly EventId GET_VERSION_START = EventIdUtil.New("OPEN_COVER_GET_VERSION_START");
		private static readonly EventId GET_VERSION_ERROR = EventIdUtil.New("OPEN_COVER_GET_VERSION_ERROR");
		private static readonly EventId GET_VERSION_SUCCESS = EventIdUtil.New("OPEN_COVER_GET_VERSION_SUCCESS");

		private static readonly EventId UPDATE_VERSION_OTHER = EventIdUtil.New("UPDATE_VERSION_OTHER");
		private static readonly EventId UPDATE_VERSION_START = EventIdUtil.New("UPDATE_VERSION_START");
		private static readonly EventId UPDATE_VERSION_ERROR = EventIdUtil.New("UPDATE_VERSION_ERROR");
		private static readonly EventId UPDATE_VERSION_SUCCESS = EventIdUtil.New("UPDATE_VERSION_SUCCESS");

		private static string OpenCoverExePath { get; set; }
		private static string AppDataOpenCoverFolder { get; set; }
		private static Version CurrentOpenCoverVersion { get; set; }
		private static HttpClient HttpClient { get; } = new HttpClient();
		private static Version MimimumOpenCoverVersion { get; } = Version.Parse("4.7.922");

		private readonly ILogger _logger;
		private readonly ServerSettings _serverSettings;
		private readonly IMsTestPlatformService _msTestPlatformService;
		
		public OpenCoverService
		(
			ServerSettings serverSettings,
			ILogger<OpenCoverService> logger,
			IMsTestPlatformService msTestPlatformService
		)
		{
			_logger = logger;
			_serverSettings = serverSettings;
			_msTestPlatformService = msTestPlatformService;
		}

		public async Task InitializeAsync()
		{
			_logger.LogInformation(INITIALIZE_START, "");

			try
			{
				AppDataOpenCoverFolder = Path.Combine(_serverSettings.AppDataFolder, "openCover");

				Directory.CreateDirectory(AppDataOpenCoverFolder);

				await GetVersionAsync();

				if (CurrentOpenCoverVersion == null)
				{
					await InstallAsync();
				}
				else if (CurrentOpenCoverVersion < MimimumOpenCoverVersion)
				{
					await UpdateVersionAsync();
				}

				_logger.LogInformation(INITIALIZE_SUCCESS, "AppDataOpenCoverFolder {AppDataOpenCoverFolder}", AppDataOpenCoverFolder);
			}
			catch (Exception exception)
			{
				_logger.LogError(INITIALIZE_ERROR, exception, "");
				throw;
			}
		}

		public async Task<Version> GetVersionAsync()
		{
			_logger.LogInformation(GET_VERSION_START, "");

			try
			{
				var exeFileName = "OpenCover.Console.exe";

				OpenCoverExePath = Directory
				.GetFiles(AppDataOpenCoverFolder, exeFileName, SearchOption.AllDirectories)
				.FirstOrDefault();

				if (string.IsNullOrWhiteSpace(OpenCoverExePath))
				{
					return null;
				}

				var nuspecFileName = "OpenCover.nuspec";

				var nuspecFilePath = Directory.GetFiles(AppDataOpenCoverFolder, nuspecFileName, SearchOption.TopDirectoryOnly).FirstOrDefault();

				if (string.IsNullOrWhiteSpace(OpenCoverExePath))
				{
					return null;
				}

				var nuspecXmlText = await File.ReadAllTextAsync(nuspecFilePath);
				var nuspecXml = XElement.Parse(nuspecXmlText);
				var versionText = nuspecXml
					?.Elements()
					?.FirstOrDefault()
					?.Elements()
					?.FirstOrDefault(x => x.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase))
					?.Value
					?.Trim();

				var versionParsed = Version.TryParse(versionText, out var version);

				if (!versionParsed)
				{
					return null;
				}

				CurrentOpenCoverVersion = version;

				_logger.LogInformation(GET_VERSION_SUCCESS, "Version {Version}", version);

				return CurrentOpenCoverVersion;
			}
			catch (Exception exception)
			{
				_logger.LogError(GET_VERSION_ERROR, exception, "");
				throw;
			}
		}

		public async Task<Version> UpdateVersionAsync()
		{
			_logger.LogInformation(UPDATE_VERSION_START, "");

			try
			{
				if (Directory.Exists(AppDataOpenCoverFolder))
				{
					Directory.Delete(AppDataOpenCoverFolder, true);
				}

				var version = await InstallAsync();

				_logger.LogInformation(UPDATE_VERSION_SUCCESS, "Version {Version}", version);

				return version;
			}
			catch (Exception exception)
			{
				_logger.LogError(UPDATE_VERSION_ERROR, exception, "");
				throw;
			}
		}

		public async Task<Version> InstallAsync()
		{
			_logger.LogInformation(INSTALL_START, "");

			try
			{
				Directory.CreateDirectory(AppDataOpenCoverFolder);

				// download

				var zipFile = Path.Combine(AppDataOpenCoverFolder, "bundle.zip");
				var url = $"https://www.nuget.org/api/v2/package/OpenCover/{MimimumOpenCoverVersion}";

				using (var remoteStream = await HttpClient.GetStreamAsync(url))
				using (var localStream = File.OpenWrite(zipFile))
				{
					await remoteStream.CopyToAsync(localStream);
				}

				// extract and cleanup

				ZipFile.ExtractToDirectory(zipFile, AppDataOpenCoverFolder);
				File.Delete(zipFile);

				// return

				var version = await GetVersionAsync();

				_logger.LogInformation(INSTALL_SUCCESS, "Version {Version}", version);

				return version;
			}
			catch (Exception exception)
			{
				_logger.LogError(INSTALL_ERROR, exception, "");
				throw;
			}
		}

		public async Task RunOpenCoverAsync(CoverageProject project)
		{
			_logger.LogInformation(RUN_START, "");

			try
			{
				if (File.Exists(project.CoverageOutputFile))
				{
					File.Delete(project.CoverageOutputFile);
				}

				if (Directory.Exists(project.CoverageOutputFolder))
				{
					Directory.Delete(project.CoverageOutputFolder, true);
				}

				Directory.CreateDirectory(project.CoverageOutputFolder);

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

					opencoverSettings.Add($@" ""-target:{_msTestPlatformService.GetMsTestPlatformExePath()}"" ");
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

					foreach (var referenceProjectWithExcludeAttribute in project.ReferencedProjects.Where(x => x.HasExcludeFromCodeCoverageAssemblyAttribute))
					{
						filters.Add($@"-[{referenceProjectWithExcludeAttribute.AssemblyName}]*");
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

				_logger.LogInformation(RUN_OTHER, "Arguments {Arguments}", opencoverSettings);

				var result = await ProcessUtil.ExecuteAsync(
					FilePath: OpenCoverExePath,
					Arguments: string.Join(" ", opencoverSettings),
					WorkingDirectory: project.ProjectOutputFolder
				);

				_logger.LogInformation(RUN_OTHER, "ExitCode {ExitCode}", result.ExitCode);

				if (result.ExitCode != 0)
				{
					throw new Exception(result.Output);
				}

				_logger.LogInformation(RUN_SUCCESS, "Output {Output}", result.Output);
			}
			catch (Exception exception)
			{
				_logger.LogError(RUN_ERROR, exception, "");
				throw;
			}
		}
	}
}
