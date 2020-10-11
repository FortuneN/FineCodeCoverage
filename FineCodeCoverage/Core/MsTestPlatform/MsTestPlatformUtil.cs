using FineCodeCoverage.Engine.Utilities;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
	internal class MsTestPlatformUtil
	{
		public static string MsTestPlatformExePath { get; private set; }
		private static HttpClient HttpClient { get; } = new HttpClient();
		public static string AppDataMsTestPlatformFolder { get; private set; }
		public static Version CurrentMsTestPlatformVersion { get; private set; }
		public static Version MimimumMsTestPlatformVersion { get; } = Version.Parse("16.7.1");

		public static void Initialize(string appDataFolder)
		{
			AppDataMsTestPlatformFolder = Path.Combine(appDataFolder, "msTestPlatform");
			Directory.CreateDirectory(AppDataMsTestPlatformFolder);
			GetMsTestPlatformVersion();

			if (CurrentMsTestPlatformVersion == null)
			{
				InstallMsTestPlatform();
			}
			else if (CurrentMsTestPlatformVersion < MimimumMsTestPlatformVersion)
			{
				UpdateMsTestPlatform();
			}
		}

		public static Version GetMsTestPlatformVersion()
		{
			var title = "MsTestPlatform Get Info";

			MsTestPlatformExePath = Directory
				.GetFiles(AppDataMsTestPlatformFolder, "vstest.console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(MsTestPlatformExePath))
			{
				Logger.Log($"{title} Not Installed");
				return null;
			}

			var nuspecFile = Directory.GetFiles(AppDataMsTestPlatformFolder, "Microsoft.TestPlatform.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();

			if (string.IsNullOrWhiteSpace(MsTestPlatformExePath))
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

			CurrentMsTestPlatformVersion = version;

			return CurrentMsTestPlatformVersion;
		}

		public static void UpdateMsTestPlatform()
		{
			var title = "MsTestPlatform Update";

			try
			{
				if (Directory.Exists(AppDataMsTestPlatformFolder))
				{
					Directory.Delete(AppDataMsTestPlatformFolder);
				}

				InstallMsTestPlatform();
			}
			catch (Exception exception)
			{
				Logger.Log(title, $"Error {exception}");
			}
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		public static void InstallMsTestPlatform()
		{
			var title = "MsTestPlatform Install";

			try
			{
				Directory.CreateDirectory(AppDataMsTestPlatformFolder);

				// download

				var zipFile = Path.Combine(AppDataMsTestPlatformFolder, "bundle.zip");
				var url = $"https://www.nuget.org/api/v2/package/Microsoft.TestPlatform/{MimimumMsTestPlatformVersion}";
				
				using (var remoteStream = HttpClient.GetStreamAsync(url).GetAwaiter().GetResult())
				using (var localStream = File.OpenWrite(zipFile))
				{
					remoteStream.CopyToAsync(localStream).GetAwaiter().GetResult();
				}

				// extract and cleanup

				ZipFile.ExtractToDirectory(zipFile, AppDataMsTestPlatformFolder);
				File.Delete(zipFile);

				// process

				GetMsTestPlatformVersion();

				// report

				Logger.Log(title, $"Installed version {CurrentMsTestPlatformVersion}");
			}
			catch (Exception exception)
			{
				Logger.Log(title, $"Error {exception}");
			}
		}
	}
}
