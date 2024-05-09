using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Blazor
{
    [Export(typeof(ICoverageContentType))]
    [Export(typeof(IFileExcluder))]
    internal class BlazorCoverageContentType : ICoverageContentType, IFileExcluder
    {
        [ImportingConstructor]
        public BlazorCoverageContentType(
            IBlazorFileCodeSpanRangeService blazorFileCodeSpanRangeService,
            IAppOptionsProvider appOptionsProvider
        )
        {
            this.blazorFileCodeSpanRangeService = blazorFileCodeSpanRangeService;
            this.appOptionsProvider = appOptionsProvider;
        }

        public const string ContentType = "Razor";
        private readonly IBlazorFileCodeSpanRangeService blazorFileCodeSpanRangeService;
        private readonly IAppOptionsProvider appOptionsProvider;

        public string ContentTypeName => ContentType;

        public IFileCodeSpanRangeService FileCodeSpanRangeService => this.blazorFileCodeSpanRangeService;

        public bool CoverageOnlyFromFileCodeSpanRangeService => this.appOptionsProvider.Get().BlazorCoverageLinesFromGeneratedSource;

        // Unfortunately, the generated docuent from the workspace is not up to date
        public bool UseFileCodeSpanRangeServiceForChanges => false;

        public ILineExcluder LineExcluder { get; } = new LineExcluder(
            CSharpCoverageContentType.Exclusions.Concat(new string[] { "<", "@" }).ToArray()
        );

        public bool Exclude(string filePath) => Path.GetExtension(filePath) != ".razor";
    }
}
