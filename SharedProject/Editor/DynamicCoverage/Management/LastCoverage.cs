using System;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    internal class LastCoverage : ILastCoverage

#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public LastCoverage(IFileLineCoverage fileLineCoverage, DateTime testExecutionStartingDate)
        {
            this.FileLineCoverage = fileLineCoverage;
            this.TestExecutionStartingDate = testExecutionStartingDate;
        }
        public IFileLineCoverage FileLineCoverage { get; }
        public DateTime TestExecutionStartingDate { get; }

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj) => obj is LastCoverage coverage && this.FileLineCoverage == coverage.FileLineCoverage && this.TestExecutionStartingDate == coverage.TestExecutionStartingDate;
    }
}
