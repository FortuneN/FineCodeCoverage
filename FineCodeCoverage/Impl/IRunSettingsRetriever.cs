using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    internal interface IRunSettingsRetriever
    {
		Task<string> GetRunSettingsFileAsync(object userSettings, object testContainer);

	}
}
