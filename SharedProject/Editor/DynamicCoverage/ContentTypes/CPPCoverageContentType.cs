using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction;

namespace FineCodeCoverage.Editor.DynamicCoverage.ContentTypes
{
    [Export(typeof(ICoverageContentType))]
    internal class CPPCoverageContentType : ICoverageContentType
    {
        public const string ContentType = "C/C++";
        public string ContentTypeName => ContentType;

        /*
            todo - https://learn.microsoft.com/en-us/previous-versions/t41260xs(v=vs.140)
            non C++ https://learn.microsoft.com/en-us/dotnet/api/envdte80.filecodemodel2?view=visualstudiosdk-2022
        */
        public IFileCodeSpanRangeService FileCodeSpanRangeService => null;

        // not relevant
        [ExcludeFromCodeCoverage]
        public bool CoverageOnlyFromFileCodeSpanRangeService => false;
        [ExcludeFromCodeCoverage]
        public bool UseFileCodeSpanRangeServiceForChanges => false;

        public ILineExcluder LineExcluder => null;
    }
}
