using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ICoverageUtilManager))]
    internal class CoverageUtilManager : ICoverageUtilManager
    {
        private readonly IOpenCoverUtil openCoverUtil;
        private readonly ICoverletUtil coverletUtil;

        [ImportingConstructor]
        public CoverageUtilManager(IOpenCoverUtil openCoverUtil, ICoverletUtil coverletUtil)
        {
            this.openCoverUtil = openCoverUtil;
            this.coverletUtil = coverletUtil;
        }

        public string CoverageToolName(ICoverageProject project)
        {
            return project.IsDotNetSdkStyle() ? "Coverlet" : "OpenCover";
        }

        public void Initialize(string appDataFolder, CancellationToken cancellationToken)
        {
            openCoverUtil.Initialize(appDataFolder, cancellationToken);
            coverletUtil.Initialize(appDataFolder, cancellationToken);
        }

        public async Task RunCoverageAsync(ICoverageProject project, CancellationToken cancellationToken)
        {
            if (project.IsDotNetSdkStyle())
            {
                await coverletUtil.RunCoverletAsync(project, cancellationToken);
            }
            else
            {
                await openCoverUtil.RunOpenCoverAsync(project, cancellationToken);
            }
        }
    }
}
