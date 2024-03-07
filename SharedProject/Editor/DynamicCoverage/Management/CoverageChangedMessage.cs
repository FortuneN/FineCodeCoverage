
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageChangedMessage
    {
        public IBufferLineCoverage BufferLineCoverage { get; }
        public string AppliesTo { get; }
        public IEnumerable<int> ChangedLineNumbers { get; }

        public CoverageChangedMessage(IBufferLineCoverage bufferLineCoverage, string appliesTo, IEnumerable<int> changedLineNumbers)
        {
            this.BufferLineCoverage = bufferLineCoverage;
            this.AppliesTo = appliesTo;
            this.ChangedLineNumbers = changedLineNumbers;
        }
    }
}
