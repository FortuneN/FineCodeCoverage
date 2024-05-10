using System;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface ILastCoverage
    {
        IFileLineCoverage FileLineCoverage { get; }
        DateTime TestExecutionStartingDate { get; }
    }
}
