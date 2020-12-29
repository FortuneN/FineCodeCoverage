namespace FineCodeCoverage.Impl
{
    internal interface IRunSettingsResultsDirectoryForEnabledCollectorParser
    {
		string Get(string runSettingsPath, string defaultTestResultsDirectory);
    }
}
