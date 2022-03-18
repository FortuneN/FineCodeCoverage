using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal interface ICoverageProjectRunSettings
    {
        ICoverageProject CoverageProject { get; set; }
        string RunSettings { get; set; }
    }
}
