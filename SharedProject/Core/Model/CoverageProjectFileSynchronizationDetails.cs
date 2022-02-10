using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverage.Engine.Model
{
    internal class CoverageProjectFileSynchronizationDetails
    {
        public List<string> Logs { get; set; } = new List<string>();
        public TimeSpan Duration { get; set; }
    }
}
