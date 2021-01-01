using FineCodeCoverage.Core.Model;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Core.MsTestPlatform
{
	public class MsTestPlatformService : IMsTestPlatformService
	{
		private static string MsTestPlatformExePath { get; set; }
		private static HttpClient HttpClient { get; } = new HttpClient();
		private static string AppDataMsTestPlatformFolder { get; set; }
		private static Version CurrentMsTestPlatformVersion { get; set; }
		private static Version MimimumMsTestPlatformVersion { get; } = Version.Parse("16.7.1");

		private readonly ServerSettings _serverSettings;

		public MsTestPlatformService(ServerSettings serverSettings)
		{
			_serverSettings = serverSettings;
		}

		public async Task InitializeAsync()
		{
			AppDataMsTestPlatformFolder = Path.Combine(_serverSettings.AppDataFolder, "msTestPlatform");
			Directory.CreateDirectory(AppDataMsTestPlatformFolder);
			await GetMsTestPlatformVersionAsync();

			if (CurrentMsTestPlatformVersion == null)
			{
				await InstallMsTestPlatformAsync();
			}
			else if (CurrentMsTestPlatformVersion < MimimumMsTestPlatformVersion)
			{
				await UpdateMsTestPlatformAsync();
			}
		}

		public string GetMsTestPlatformExePath()
		{
			return MsTestPlatformExePath;
		}

		public async Task<Version> GetMsTestPlatformVersionAsync()
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

			var nuspecXmlText = await File.ReadAllTextAsync(nuspecFile);
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

		public async Task UpdateMsTestPlatformAsync()
		{
			var title = "MsTestPlatform Update";

			try
			{
				if (Directory.Exists(AppDataMsTestPlatformFolder))
				{
					Directory.Delete(AppDataMsTestPlatformFolder, true);
				}

				await InstallMsTestPlatformAsync();
			}
			catch (Exception exception)
			{
				Logger.Log(title, $"Error {exception}");
			}
		}

		public async Task InstallMsTestPlatformAsync()
		{
			var title = "MsTestPlatform Install";

			try
			{
				Directory.CreateDirectory(AppDataMsTestPlatformFolder);

				// download

				var zipFile = Path.Combine(AppDataMsTestPlatformFolder, "bundle.zip");
				var url = $"https://www.nuget.org/api/v2/package/Microsoft.TestPlatform/{MimimumMsTestPlatformVersion}";
				
				using (var remoteStream = await HttpClient.GetStreamAsync(url))
				using (var localStream = File.OpenWrite(zipFile))
				{
					await remoteStream.CopyToAsync(localStream);
				}

				// extract and cleanup

				ZipFile.ExtractToDirectory(zipFile, AppDataMsTestPlatformFolder);
				File.Delete(zipFile);

				// process

				await GetMsTestPlatformVersionAsync();

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
