using System;
using System.ComponentModel.Composition;
using EnvDTE;
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
		private DTE dte;

        [ImportingConstructor]
		public CoverageProjectFactory(
			IAppOptionsProvider appOptionsProvider,
			IFileSynchronizationUtil fileSynchronizationUtil, 
			ILogger logger,
			[Import(typeof(SVsServiceProvider))]
			IServiceProvider serviceProvider)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.fileSynchronizationUtil = fileSynchronizationUtil;
            this.logger = logger;
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				dte = (DTE)serviceProvider.GetService(typeof(DTE));
			});
		}

        public void Initialize()
        {
            MSBuildLocator.RegisterDefaults();
        }
        public CoverageProject Create()
        {
			return new CoverageProject(appOptionsProvider,fileSynchronizationUtil, logger, dte);
        }
    }
}
