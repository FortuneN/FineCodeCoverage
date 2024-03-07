using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Core.Initialization
{
    interface IInitializable { }

    [Export(typeof(IInitializer))]
    [Export(typeof(IInitializeStatusProvider))]
    internal class Initializer : IInitializer
    {
        private readonly IFCCEngine fccEngine;
        private readonly ILogger logger;
        private readonly ICoverageProjectFactory coverageProjectFactory;
        private readonly IFirstTimeToolWindowOpener firstTimeToolWindowOpener;

        public InitializeStatus InitializeStatus { get; set; } = InitializeStatus.Initializing;
        public string InitializeExceptionMessage { get; set; }

        [ImportingConstructor]
        public Initializer(
            IFCCEngine fccEngine, 
            ILogger logger, 
            ICoverageProjectFactory coverageProjectFactory,
            IFirstTimeToolWindowOpener firstTimeToolWindowOpener,
            [ImportMany]
            IInitializable[] initializables
        )
        {
            this.fccEngine = fccEngine;
            this.logger = logger;
            this.coverageProjectFactory = coverageProjectFactory;
            this.firstTimeToolWindowOpener = firstTimeToolWindowOpener;
        }
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                logger.Log($"Initializing");

                cancellationToken.ThrowIfCancellationRequested();
                coverageProjectFactory.Initialize();

                fccEngine.Initialize(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                logger.Log($"Initialized");

                
                await firstTimeToolWindowOpener.OpenIfFirstTimeAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                InitializeStatus = InitializeStatus.Error;
                InitializeExceptionMessage = exception.Message;
                if (!cancellationToken.IsCancellationRequested)
                {
                    logger.Log($"Failed Initialization", exception);
                }
            }

            if(InitializeStatus != InitializeStatus.Error)
            {
                InitializeStatus = InitializeStatus.Initialized;
            }
        }
        
    }

}

