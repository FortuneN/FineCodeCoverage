using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Cobertura
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IFileLineCoverageFactory))]
    internal class FileLineCoverageFactory : IFileLineCoverageFactory
    {
        public IFileLineCoverage Create() => new FileLineCoverage();
    }
}
