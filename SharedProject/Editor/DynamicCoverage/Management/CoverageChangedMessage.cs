
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal class CoverageChangedMessage
    {
        public IBufferLineCoverage CoverageLines { get; }
        public string AppliesTo { get; }
        public IEnumerable<int> ChangedLineNumbers { get; }

        public CoverageChangedMessage(IBufferLineCoverage coverageLines, string appliesTo, IEnumerable<int> changedLineNumbers)
        {
            this.CoverageLines = coverageLines;
            this.AppliesTo = appliesTo;
            this.ChangedLineNumbers = changedLineNumbers;
        }
    }
}
