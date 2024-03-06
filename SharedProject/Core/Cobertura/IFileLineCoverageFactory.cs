using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    internal interface IFileLineCoverageFactory
    {
        IFileLineCoverage Create();
    }
}
