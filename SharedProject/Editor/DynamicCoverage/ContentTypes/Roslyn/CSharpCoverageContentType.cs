using System.ComponentModel.Composition;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes.Roslyn
{
    [Export(typeof(ICoverageContentType))]
    internal class CSharpCoverageContentType : ICoverageContentType
    {
        [ImportingConstructor]
        public CSharpCoverageContentType(IRoslynFileCodeSpanRangeService roslynFileCodeSpanRangeService) 
            => this.roslynFileCodeSpanRangeService = roslynFileCodeSpanRangeService;

        public const string ContentType = "CSharp";
        private readonly IRoslynFileCodeSpanRangeService roslynFileCodeSpanRangeService;

        public string ContentTypeName => ContentType;

        public IFileCodeSpanRangeService FileCodeSpanRangeService 
            => this.roslynFileCodeSpanRangeService.FileCodeSpanRangeService;

        public bool UseFileCodeSpanRangeServiceForChanges 
            => this.roslynFileCodeSpanRangeService.UseFileCodeSpanRangeServiceForChanges;

        public bool CoverageOnlyFromFileCodeSpanRangeService => false;

        public static string[] Exclusions { get; } = new string[] { "//", "#", "using" };

        public ILineExcluder LineExcluder { get; } = new LineExcluder(Exclusions);

    }
}
