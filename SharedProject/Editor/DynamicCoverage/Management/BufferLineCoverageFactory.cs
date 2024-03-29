﻿using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IBufferLineCoverageFactory))]
    internal class BufferLineCoverageFactory : IBufferLineCoverageFactory
    {
        private readonly IDynamicCoverageStore dynamicCoverageStore;
        private readonly IAppOptionsProvider appOptionsProvider;

        [ImportingConstructor]
        public BufferLineCoverageFactory(
            IDynamicCoverageStore dynamicCoverageStore,
            IAppOptionsProvider appOptionsProvider
        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.dynamicCoverageStore = dynamicCoverageStore;
        }

        public IBufferLineCoverage Create(
            IFileLineCoverage fileLineCoverage,
            ITextInfo textInfo,
            IEventAggregator eventAggregator,
            ITrackedLinesFactory trackedLinesFactory
        ) => new BufferLineCoverage(
                fileLineCoverage,
                textInfo,
                eventAggregator,
                trackedLinesFactory,
                this.dynamicCoverageStore,
                this.appOptionsProvider);
    }
}
