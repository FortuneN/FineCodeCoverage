using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    [Export(typeof(IMsTestPlatformUtil))]
	internal class MsTestPlatformUtil:IMsTestPlatformUtil
	{
		public string MsTestPlatformExePath { get; private set; }
        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
		private const string zipPrefix = "microsoft.testplatform";
		private const string zipDirectoryName = "msTestPlatform";

		[ImportingConstructor]
		public MsTestPlatformUtil(IToolFolder toolFolder, IToolZipProvider toolZipProvider)
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
        }
		public void Initialize(string appDataFolder)
		{
			var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix));
			MsTestPlatformExePath = Directory
				.GetFiles(zipDestination, "vstest.console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();
		}
	}
}
