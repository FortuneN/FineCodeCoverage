using System.ComponentModel.Composition;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn
{
    [Export(typeof(ICoverageContentType))]
    internal class VBCoverageContentType : ICoverageContentType
    {
        [ImportingConstructor]
        public VBCoverageContentType(IRoslynFileCodeSpanRangeService roslynFileCodeSpanRangeService)
            => this.roslynFileCodeSpanRangeService = roslynFileCodeSpanRangeService;

        public const string ContentType = "Basic";
        private readonly IRoslynFileCodeSpanRangeService roslynFileCodeSpanRangeService;

        public string ContentTypeName => ContentType;

        public IFileCodeSpanRangeService FileCodeSpanRangeService
            => this.roslynFileCodeSpanRangeService.FileCodeSpanRangeService;

        public bool CoverageOnlyFromFileCodeSpanRangeService => false;

        public bool UseFileCodeSpanRangeServiceForChanges
            => this.roslynFileCodeSpanRangeService.UseFileCodeSpanRangeServiceForChanges;

        public ILineExcluder LineExcluder { get; } = new LineExcluder(new string[] { "REM", "'", "#" });
    }
}
