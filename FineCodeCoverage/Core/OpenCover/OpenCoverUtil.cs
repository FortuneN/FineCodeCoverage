//using FineCodeCoverage.Engine.Model;
//using FineCodeCoverage.Engine.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Diagnostics.CodeAnalysis;
//using System.IO;
//using System.IO.Compression;
//using System.IO.Packaging;
//using System.Linq;
//using System.Reflection;
//using System.Threading.Tasks;

//namespace FineCodeCoverage.Engine.OpenCover
//{
//	internal class OpenCoverUtil
//	{
//		public const string OpenCoverName = "OpenCover";
//		public static string OpenCoverExePath { get; private set; }
//		public static string AppDataOpenCoverFolder { get; private set; }
//		public static Version CurrentOpenCoverVersion { get; private set; }
//		public static Version MimimumOpenCoverVersion { get; } = Version.Parse("4.7.922");

//		public static void Initialize(string appDataFolder)
//		{
//			AppDataOpenCoverFolder = Path.Combine(appDataFolder, "openCover");
//			Directory.CreateDirectory(AppDataOpenCoverFolder);
//			GetOpenCoverVersion();

//			if (CurrentOpenCoverVersion == null)
//			{
//				InstallOpenCover();
//			}
//			else if (CurrentOpenCoverVersion < MimimumOpenCoverVersion)
//			{
//				UpdateOpenCover();
//			}
//		}

//		public static Version GetOpenCoverVersion()
//		{
//			var title = "OpenCover Get Info";

//			OpenCoverExePath = Directory.GetFiles(AppDataOpenCoverFolder, "OpenCover.Console.exe", SearchOption.AllDirectories).FirstOrDefault();

//			var processStartInfo = new ProcessStartInfo
//			{
//				FileName = OpenCoverExePath,
//				CreateNoWindow = true,
//				UseShellExecute = false,
//				RedirectStandardError = true,
//				RedirectStandardOutput = true,
//				WindowStyle = ProcessWindowStyle.Hidden,
//				Arguments = $"-version",
//			};

//			var process = Process.Start(processStartInfo);

//			process.WaitForExit();

//			var processOutput = process.GetOutput().Trim(' ', '\r', '\n', '\t');

//			var versionParsed = Version.TryParse(
//				processOutput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last(),
//				out var version
//			);

//			if (!versionParsed)
//			{
//				Logger.Log($"{title} Error", processOutput);
//				return null;
//			}

//			CurrentOpenCoverVersion = version;
			
//			return CurrentOpenCoverVersion;
//		}

//		public static void UpdateOpenCover()
//		{
//			var title = "OpenCover Update";

//			try
//			{
//				if (Directory.Exists(AppDataOpenCoverFolder))
//				{
//					Directory.Delete(AppDataOpenCoverFolder);
//				}

//				InstallOpenCover();
//			}
//			catch (Exception exception)
//			{
//				Logger.Log(title, $"Error {exception}");
//			}
//		}

//		public static void InstallOpenCover()
//		{
//			var title = "OpenCover Install";

//			try
//			{
//				var rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
//				var zipFile = Path.Combine(rootFolder, "Resources", "opencover.zip");

//				Directory.CreateDirectory(AppDataOpenCoverFolder);
//				ZipFile.ExtractToDirectory(zipFile, AppDataOpenCoverFolder);

//				GetOpenCoverVersion();

//				Logger.Log(title, $"Installed version {CurrentOpenCoverVersion}");
//			}
//			catch (Exception exception)
//			{
//				Logger.Log(title, $"Error {exception}");
//			}
//		}

//		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
//		public static bool RunOpenCover(CoverageProject project, bool throwError = false)
//		{
//			var title = $"OpenCover Run ({project.ProjectName})";

//			if (File.Exists(project.CoverOutputFile))
//			{
//				File.Delete(project.CoverOutputFile);
//			}

//			if (Directory.Exists(project.WorkOutputFolder))
//			{
//				Directory.Delete(project.WorkOutputFolder, true);
//			}

//			Directory.CreateDirectory(project.WorkOutputFolder);

//			//OpenCover.Console.exe -register:user -target:Samples\x64\OpenCover.Simple.Target.exe -filter:+[*]* -output:output64.xml

//			var opencoverSettings = new List<string>();

//			opencoverSettings.Add($@"""-target:{project.TestDllFileInWorkFolder}""");

//			opencoverSettings.Add($@"""-register:user""");

//			//opencoverSettings.Add($@"--format ""cobertura""");

//			//foreach (var value in (project.Settings.Exclude ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			//{
//			//	opencoverSettings.Add($@"--exclude ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
//			//}

//			//foreach (var value in (project.Settings.Include ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			//{
//			//	opencoverSettings.Add($@"--include ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
//			//}

//			//foreach (var value in (project.Settings.IncludeDirectory ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			//{
//			//	opencoverSettings.Add($@"--include-directory ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
//			//}

//			//foreach (var value in (project.Settings.ExcludeByFile ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			//{
//			//	opencoverSettings.Add($@"--exclude-by-file ""{value.Replace("\"", "\\\"").Trim(' ', '\'')}""");
//			//}

//			//foreach (var value in (project.Settings.ExcludeByAttribute ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			//{
//			//	opencoverSettings.Add($@"--exclude-by-attribute ""{value.Replace("\"", "\\\"").Trim(' ', '\'', '[', ']')}""");
//			//}

//			//if (project.Settings.IncludeTestAssembly)
//			//{
//			//	opencoverSettings.Add("--include-test-assembly");
//			//}

//			////https://github.com/opencover-coverage/opencover/issues/961
//			////if (appSettings.SkipAutoProperties)
//			////{
//			////	opencoverSettings.Add("--skipautoprops");
//			////}

//			////https://github.com/opencover-coverage/opencover/issues/962
//			////foreach (var value in (appSettings.DoesNotReturnAttributes ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)))
//			////{
//			////	opencoverSettings.Add($@"--does-not-return-attribute ""{value.Replace("\"", "\\\"")}""");
//			////}

//			//opencoverSettings.Add($@"--target ""dotnet""");

//			opencoverSettings.Add($@"""-output:{ project.CoverOutputFile }""");

//			Logger.Log($"{title} Arguments {Environment.NewLine}{string.Join($"{Environment.NewLine}", opencoverSettings)}");

//			var processStartInfo = new ProcessStartInfo
//			{
//				FileName = OpenCoverExePath,
//				CreateNoWindow = true,
//				UseShellExecute = false,
//				RedirectStandardError = true,
//				RedirectStandardOutput = true,
//				WindowStyle = ProcessWindowStyle.Hidden,
//				Arguments = string.Join(" ", opencoverSettings),
//			};

//			var process = Process.Start(processStartInfo);

//			if (!process.HasExited)
//			{
//				var stopWatch = new Stopwatch();
//				stopWatch.Start();

//				if (!Task.Run(() => process.WaitForExit()).Wait(TimeSpan.FromSeconds(project.Settings.CoverletTimeout)))
//				{
//					stopWatch.Stop();
//					Task.Run(() => { try { process.Kill(); } catch { } }).Wait(TimeSpan.FromSeconds(10));

//					var errorMessage = $"OpenCover timed out after {stopWatch.Elapsed.TotalSeconds} seconds ({nameof(project.Settings.CoverletTimeout)} is {project.Settings.CoverletTimeout} seconds)";

//					if (throwError)
//					{
//						throw new Exception(errorMessage);
//					}

//					Logger.Log($"{title} Error", errorMessage);
//					return false;
//				}
//			}

//			var processOutput = process.GetOutput();

//			if (process.ExitCode != 0)
//			{
//				if (throwError)
//				{
//					throw new Exception(processOutput);
//				}

//				Logger.Log($"{title} Error", processOutput);
//				return false;
//			}

//			Logger.Log(title, processOutput);
//			return true;
//		}
//	}
//}
