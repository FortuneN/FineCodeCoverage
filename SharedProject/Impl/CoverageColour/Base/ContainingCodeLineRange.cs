using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    internal class ContainingCodeLineRange
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public bool ContainsAny(List<int> lineNumbers)
        {
            return lineNumbers.Any(lineNumber => lineNumber >= StartLine && lineNumber <= EndLine);
        }
    }
}
