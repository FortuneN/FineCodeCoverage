namespace FineCodeCoverage.Engine.Model
{
    internal interface ILine
    {
        int Number { get; }
        CoverageType CoverageType { get; }
    }
}
