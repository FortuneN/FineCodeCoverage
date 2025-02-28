using System.ComponentModel.Composition;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectFactory))]
    internal class CoverageProjectFactory : ICoverageProjectFactory
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ICoverageProjectSettingsManager coverageProjectSettingsManager;
        private readonly IReferencedProjectsHelper referencedProjectsHelper;

        [ImportingConstructor]
		public CoverageProjectFactory(
			IAppOptionsProvider appOptionsProvider,
			IFileSynchronizationUtil fileSynchronizationUtil, 
            ICoverageProjectSettingsManager coverageProjectSettingsManager,
            IReferencedProjectsHelper referencedProjectsHelper
            )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.coverageProjectSettingsManager = coverageProjectSettingsManager;
            this.referencedProjectsHelper = referencedProjectsHelper;
        }

        public ICoverageProject Create()
        {
            return new CoverageProject(
                appOptionsProvider,
                fileSynchronizationUtil, 
                coverageProjectSettingsManager,
                referencedProjectsHelper);
        }
    }
}
