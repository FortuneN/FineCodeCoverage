namespace FineCodeCoverage.Impl
{
    internal interface ICoverletCoberturaResultsDirectoryCollectorFactory
	{
		ICoverletCoberturaResultsDirectoryCollector Create(string resultsDirectory);
	}
}
