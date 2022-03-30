using System;
using System.ComponentModel.Composition;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using FineCodeCoverage.Engine.FileSynchronization;
using FineCodeCoverage.Options;
using Microsoft.Build.Locator;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectFactory))]
    internal class CoverageProjectFactory : ICoverageProjectFactory
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IFileSynchronizationUtil fileSynchronizationUtil;
        private readonly ILogger logger;
        private readonly ICoverageProjectSettingsManager coverageProjectSettingsManager;
        private readonly DTE2 dte;
        private bool canUseMsBuildWorkspace = true;

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
            ThreadHelper.ThrowIfNotOnUIThread();
            dte = (DTE2)serviceProvider.GetService(typeof(DTE));
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
        public ICoverageProject Create()
        {
			return new CoverageProject(
                appOptionsProvider,
                fileSynchronizationUtil, 
                logger, 
                dte, 
                coverageProjectSettingsManager,
                canUseMsBuildWorkspace);
        }
    }
}
