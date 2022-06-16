using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;
using Microsoft.Build.Locator;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectFactory))]
    internal class CoverageProjectFactory : ICoverageProjectFactory
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ILogger logger;
        private readonly ICoverageProjectSettingsManager coverageProjectSettingsManager;
        private bool canUseMsBuildWorkspace = true;
        private readonly AsyncLazy<DTE2> lazyDTE2;

        [ImportingConstructor]
		public CoverageProjectFactory(
			IAppOptionsProvider appOptionsProvider,
			IFileSynchronizationUtil fileSynchronizationUtil, 
			ILogger logger,
            ICoverageProjectSettingsManager coverageProjectSettingsManager,
            [Import(typeof(SVsServiceProvider))]
			IServiceProvider serviceProvider)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.logger = logger;
            this.coverageProjectSettingsManager = coverageProjectSettingsManager;

            lazyDTE2 = new AsyncLazy<DTE2>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (DTE2)serviceProvider.GetService(typeof(DTE));
            },ThreadHelper.JoinableTaskFactory);
        }

        public void Initialize()
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch
            {
                canUseMsBuildWorkspace = false;
            }
        }
        public async Task<ICoverageProject> CreateAsync()
        {
            var dte2 = await lazyDTE2.GetValueAsync();

            return new CoverageProject(
                appOptionsProvider,
                fileSynchronizationUtil, 
                logger, 
                dte2, 
                coverageProjectSettingsManager,
                canUseMsBuildWorkspace);
        }
    }
}
