using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using System.IO.Compression;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
	interface IMsTestPlatformUtil
    {
		string MsTestPlatformExePath { get; }
		void Initialize(string appDataFolder);
	}

	[Export(typeof(IMsTestPlatformUtil))]
	internal class MsTestPlatformUtil:IMsTestPlatformUtil
	{
		public string MsTestPlatformExePath { get; private set; }
		private HttpClient HttpClient { get; } = new HttpClient();
		private string appDataMsTestPlatformFolder;
		private Version currentMsTestPlatformVersion;
        private readonly ILogger logger;

        private Version MimimumMsTestPlatformVersion { get; } = Version.Parse("16.7.1");

		[ImportingConstructor]
		public MsTestPlatformUtil(ILogger logger)
        {
            this.logger = logger;
        }
		public void Initialize(string appDataFolder)
		{
			appDataMsTestPlatformFolder = Path.Combine(appDataFolder, "msTestPlatform");
			Directory.CreateDirectory(appDataMsTestPlatformFolder);
			GetMsTestPlatformVersion();

			if (currentMsTestPlatformVersion == null)
			{
				InstallMsTestPlatform();
			}
			else if (currentMsTestPlatformVersion < MimimumMsTestPlatformVersion)
			{
				UpdateMsTestPlatform();
			}
		}

		private Version GetMsTestPlatformVersion()
		{
			var title = "MsTestPlatform Get Info";

			MsTestPlatformExePath = Directory
				.GetFiles(appDataMsTestPlatformFolder, "vstest.console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();

			if (string.IsNullOrWhiteSpace(MsTestPlatformExePath))
			{
				logger.Log($"{title} Not Installed");
				return null;
			}

			var nuspecFile = Directory.GetFiles(appDataMsTestPlatformFolder, "Microsoft.TestPlatform.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();

			if (string.IsNullOrWhiteSpace(MsTestPlatformExePath))
			{
				logger.Log($"{title} Nuspec Not Found");
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
				logger.Log($"{title} Failed to parse nuspec", nuspecXmlText);
				return null;
			}

			currentMsTestPlatformVersion = version;

			return currentMsTestPlatformVersion;
		}

		public void UpdateMsTestPlatform()
		{
			var title = "MsTestPlatform Update";

			try
			{
				if (Directory.Exists(appDataMsTestPlatformFolder))
				{
					Directory.Delete(appDataMsTestPlatformFolder);
				}

				InstallMsTestPlatform();
			}
			catch (Exception exception)
			{
				logger.Log(title, $"Error {exception}");
			}
		}

		[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
		private void InstallMsTestPlatform()
		{
			var title = "MsTestPlatform Install";

			try
			{
				Directory.CreateDirectory(appDataMsTestPlatformFolder);

				// download

				var zipFile = Path.Combine(appDataMsTestPlatformFolder, "bundle.zip");
				var url = $"https://www.nuget.org/api/v2/package/Microsoft.TestPlatform/{MimimumMsTestPlatformVersion}";
				
				using (var remoteStream = HttpClient.GetStreamAsync(url).GetAwaiter().GetResult())
				using (var localStream = File.OpenWrite(zipFile))
				{
					remoteStream.CopyToAsync(localStream).GetAwaiter().GetResult();
				}

				// extract and cleanup

				ZipFile.ExtractToDirectory(zipFile, appDataMsTestPlatformFolder);
				File.Delete(zipFile);

				// process

				GetMsTestPlatformVersion();

				// report

				logger.Log(title, $"Installed version {currentMsTestPlatformVersion}");
			}
			catch (Exception exception)
			{
				logger.Log(title, $"Error {exception}");
			}
		}
	}
}
