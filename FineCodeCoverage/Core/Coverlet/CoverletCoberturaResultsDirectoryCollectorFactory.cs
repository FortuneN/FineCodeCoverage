using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverletCoberturaResultsDirectoryCollectorFactory))]
    internal class CoverletCoberturaResultsDirectoryCollectorFactory : ICoverletCoberturaResultsDirectoryCollectorFactory
    {
        public ICoverletCoberturaResultsDirectoryCollector Create(string resultsDirectory)
        {
			return new CoverletCoberturaResultsDirectoryCollector(resultsDirectory);
        }
    }
}
