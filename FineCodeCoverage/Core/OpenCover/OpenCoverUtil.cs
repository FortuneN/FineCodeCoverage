using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using FineCodeCoverage.Engine.Model;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Engine.Utilities;
using FineCodeCoverage.Engine.MsTestPlatform;

namespace FineCodeCoverage.Engine.OpenCover
{
	internal class OpenCoverUtil
	{
		public const string OpenCoverName = "OpenCover";
		public static string OpenCoverExePath { get; private set; }
		private static HttpClient HttpClient { get; } = new HttpClient();
		public static string AppDataOpenCoverFolder { get; private set; }
		public static Version CurrentOpenCoverVersion { get; private set; }
		public static Version MimimumOpenCoverVersion { get; } = Version.Parse("4.7.922");

		public static void Initialize(string appDataFolder)
		{
			AppDataOpenCoverFolder = Path.Combine(appDataFolder, "openCover");
			Directory.CreateDirectory(AppDataOpenCoverFolder);
			GetOpenCoverVersion();

			if (CurrentOpenCoverVersion == null)
			{
				InstallOpenCover();
			}
			else if (CurrentOpenCoverVersion < MimimumOpenCoverVersion)
			{
				UpdateOpenCover();
			}
		}

		public static Version GetOpenCoverVersion()
		{
			var title = "OpenCover Get Info";

			OpenCoverExePath = Directory
				.GetFiles(AppDataOpenCoverFolder, "OpenCover.Console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(OpenCoverExePath))
			{
				Logger.Log($"{title} Not Installed");
				return null;
			}

			var nuspecFile = Directory.GetFiles(AppDataOpenCoverFolder, "OpenCover.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();

			if (string.IsNullOrWhiteSpace(OpenCoverExePath))
			{
				Logger.Log($"{title} Nuspec Not Found");
				return null;
			}

			var nuspecXmlText = File.ReadAllText(nuspecFile);
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
				Logger.Log($"{title} Failed to parse nuspec", nuspecXmlText);
				return null;
			}

			CurrentOpenCoverVersion = version;

			return CurrentOpenCoverVersion;
		}

		public static void UpdateOpenCover()
		{
			var title = "OpenCover Update";

			try
			{
				if (Directory.Exists(AppDataOpenCoverFolder))
				{
					Directory.Delete(AppDataOpenCoverFolder);
				}

				InstallOpenCover();
			}
			catch (Exception exception)
			{
				Logger.Log(title, $"Error {exception}");
			}
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		public static void InstallOpenCover()
		{
			var title = "OpenCover Install";

			try
			{
				Directory.CreateDirectory(AppDataOpenCoverFolder);

				// download

				var zipFile = Path.Combine(AppDataOpenCoverFolder, "bundle.zip");
				var url = $"https://www.nuget.org/api/v2/package/OpenCover/{MimimumOpenCoverVersion}";
				
				using (var remoteStream = HttpClient.GetStreamAsync(url).GetAwaiter().GetResult())
				using (var localStream = File.OpenWrite(zipFile))
				{
					remoteStream.CopyToAsync(localStream).GetAwaiter().GetResult();
				}

				// extract and cleanup

				ZipFile.ExtractToDirectory(zipFile, AppDataOpenCoverFolder);
				File.Delete(zipFile);

				// process

				GetOpenCoverVersion();

				// report

				Logger.Log(title, $"Installed version {CurrentOpenCoverVersion}");
			}
			catch (Exception exception)
			{
				Logger.Log(title, $"Error {exception}");
			}
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		public static bool RunOpenCover(CoverageProject project, bool throwError = false)
		{
			var title = $"OpenCover Run ({project.ProjectName})";

			if (File.Exists(project.CoverToolOutputFile))
			{
				File.Delete(project.CoverToolOutputFile);
			}

			if (Directory.Exists(project.WorkOutputFolder))
			{
				Directory.Delete(project.WorkOutputFolder, true);
			}

			Directory.CreateDirectory(project.WorkOutputFolder);

			var opencoverSettings = new List<string>();

			opencoverSettings.Add($@" -mergebyhash ");

			opencoverSettings.Add($@" -hideskipped:all ");

			{
				// -register:

				var registerValue = "path32";

				if (project.TestDllCompilationMode.HasFlag(CompilationMode.Bit64))
				{
					registerValue = "path64";
				}

				opencoverSettings.Add($@" -register:{registerValue} ");
			}

			{
				// -target:

				opencoverSettings.Add($@" ""-target:{MsTestPlatformUtil.MsTestPlatformExePath}"" ");
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

				if (filters.Any(x => !x.Equals(defaultFilter)))
				{
					opencoverSettings.Add($@" ""-filter:{string.Join(" ", filters)}"" ");
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
				
				var testDllPdbFile = Path.Combine(project.WorkFolder, Path.GetFileNameWithoutExtension(project.TestDllFileInWorkFolder)) + ".pdb";
				File.Delete(testDllPdbFile);

				// filtering out the test-assembly blows up the entire process and nothing gets instrumented or analysed
				
				//var nameOnlyOfDll = Path.GetFileNameWithoutExtension(project.TestDllFileInWorkFolder);
				//filters.Add($@"-[{nameOnlyOfDll}]*");
			}

			opencoverSettings.Add($@" ""-targetargs:\""{project.TestDllFileInWorkFolder}\"""" ");

			opencoverSettings.Add($@" ""-output:{ project.CoverToolOutputFile }"" ");

			Logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", opencoverSettings)}");

			var result = ProcessUtil
			.ExecuteAsync(new ExecuteRequest
			{
				FilePath = OpenCoverExePath,
				Arguments = string.Join(" ", opencoverSettings),
				WorkingDirectory = project.WorkFolder
			})
			.GetAwaiter()
			.GetResult();

			if (result.ExitCode != 0)
			{
				if (throwError)
				{
					throw new Exception(result.Output);
				}

				Logger.Log($"{title} Error", result.Output);
				return false;
			}

			Logger.Log(title, result.Output);
			return true;
		}
	}
}
