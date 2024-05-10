using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IBufferLineCoverageFactory))]
    internal class BufferLineCoverageFactory : IBufferLineCoverageFactory
    {
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public BufferLineCoverageFactory(
            IDynamicCoverageStore dynamicCoverageStore,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger
        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.dynamicCoverageStore = dynamicCoverageStore;
        }

        public IBufferLineCoverage Create(
            LastCoverage lastCoverage,
            ITextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory
        ) => new BufferLineCoverage(
                lastCoverage,
                textInfo,
                eventAggregator,
                trackedLinesFactory,
                this.dynamicCoverageStore,
                this.appOptionsProvider,
                this.logger
                );
    }
}
