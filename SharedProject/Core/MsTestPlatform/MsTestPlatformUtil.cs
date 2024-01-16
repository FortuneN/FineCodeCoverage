using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;
using System.Threading;

namespace FineCodeCoverage.Engine.MsTestPlatform
{
    [Export(typeof(IMsTestPlatformUtil))]
	internal class MsTestPlatformUtil:IMsTestPlatformUtil
	{
		public string MsTestPlatformExePath { get; private set; }
        private readonly IToolUnzipper toolUnzipper;
        private const string zipPrefix = "microsoft.testplatform";
		private const string zipDirectoryName = "msTestPlatform";

		[ImportingConstructor]
		public MsTestPlatformUtil(IToolUnzipper toolUnzipper)
        {
            this.toolUnzipper = toolUnzipper;
        }
		public void Initialize(string appDataFolder, CancellationToken cancellationToken)
		{
			var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolder, zipDirectoryName, zipPrefix, cancellationToken);
			MsTestPlatformExePath = Directory
				.GetFiles(zipDestination, "vstest.console.exe", SearchOption.AllDirectories)
				.FirstOrDefault();
		}
	}
}
