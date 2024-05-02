using System.ComponentModel.Composition;
using System.IO;
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
        public BlazorCoverageContentType(IAppOptionsProvider appOptionsProvider, IBlazorFileCodeSpanRangeService blazorFileCodeSpanRangeService)
        {
            this.appOptionsProvider = appOptionsProvider;
            this.blazorFileCodeSpanRangeService = blazorFileCodeSpanRangeService;
        }

        public const string ContentType = "Razor";
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly IBlazorFileCodeSpanRangeService blazorFileCodeSpanRangeService;

        public string ContentTypeName => ContentType;

        public IFileCodeSpanRangeService FileCodeSpanRangeService => this.blazorFileCodeSpanRangeService;
        public IFileCodeSpanRangeService FileCodeSpanRangeServiceForChanges 
            => this.appOptionsProvider.Get().EditorCoverageColouringMode == EditorCoverageColouringMode.DoNotUseRoslynWhenTextChanges
                ? null
                : this.FileCodeSpanRangeService;

        public ILineExcluder LineExcluder { get; } = new DoNotExcludeLine();

        public bool Exclude(string filePath) => Path.GetExtension(filePath) != ".razor";
    }
}
