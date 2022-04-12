using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageLineTagger<TTag> : ITagger<TTag>, IListener<NewCoverageLinesMessage> where TTag : ITag
    {

    }
}
