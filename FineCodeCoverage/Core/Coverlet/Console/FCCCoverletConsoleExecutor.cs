using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{
    public interface ICoverletConsoleExeFinder
    {
        string FindInFolder(string folder, SearchOption searchOption);

    }
    public class CoverletConsoleExeFinder
    {
        public string FindInFolder(string folder, SearchOption searchOption)
        {
            return Directory.GetFiles(folder, "coverlet.exe", searchOption).FirstOrDefault()
                           ?? Directory.GetFiles(folder, "*coverlet*.exe", searchOption).FirstOrDefault();
        }
    }

    [Export(typeof(IFCCCoverletConsoleExecutor))]
    internal class FCCCoverletConsoleExecutor : IFCCCoverletConsoleExecutor
    {
		[ImportingConstructor]
		public FCCCoverletConsoleExecutor(IToolFolder toolFolder, IToolZipProvider toolZipProvider)
        {
            this.toolFolder = toolFolder;
            this.toolZipProvider = toolZipProvider;
        }

        private readonly IToolFolder toolFolder;
        private readonly IToolZipProvider toolZipProvider;
        private string coverletExePath;
		private const string zipPrefix = "coverlet.console";
		private const string zipDirectoryName = "coverlet";//backwards compatibility
		public ExecuteRequest GetRequest(ICoverageProject coverageProject, string coverletSettings)
        {
			return new ExecuteRequest
			{
				FilePath = coverletExePath,
				Arguments = coverletSettings,
				WorkingDirectory = coverageProject.ProjectOutputFolder
			};

		}

		public void Initialize(string appDataFolder)
		{
			var zipDestination = toolFolder.EnsureUnzipped(appDataFolder, zipDirectoryName, toolZipProvider.ProvideZip(zipPrefix));
			coverletExePath = Directory.GetFiles(zipDestination, "coverlet.exe", SearchOption.AllDirectories).FirstOrDefault()
						   ?? Directory.GetFiles(zipDestination, "*coverlet*.exe", SearchOption.AllDirectories).FirstOrDefault();
		}
	}
}
