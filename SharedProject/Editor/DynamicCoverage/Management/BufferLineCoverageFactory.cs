using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageContentTypes : ICoverageContentTypes
    {
        private readonly ICoverageContentType[] coverageContentTypes;

        public CoverageContentTypes(ICoverageContentType[] coverageContentTypes) 
            => this.coverageContentTypes = coverageContentTypes;
        public bool IsApplicable(string contentTypeName) 
            => this.coverageContentTypes.Any(contentType => contentType.ContentTypeName == contentTypeName);
    }

    [ExcludeFromCodeCoverage]
    [Export(typeof(IBufferLineCoverageFactory))]
    internal class BufferLineCoverageFactory : IBufferLineCoverageFactory
    {
        private readonly ICoverageContentTypes coverageContentTypes;
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public BufferLineCoverageFactory(
            [ImportMany]
            ICoverageContentType[] coverageContentTypes,
            IDynamicCoverageStore dynamicCoverageStore,
            IAppOptionsProvider appOptionsProvider,
            ILogger logger
        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.coverageContentTypes = new CoverageContentTypes(coverageContentTypes);
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
                this.coverageContentTypes,
                this.logger

                );
    }
}
