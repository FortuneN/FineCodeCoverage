using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    internal interface ITestConfiguration
    {
		string ResultsDirectory { get; }
		object UserRunSettings { get; }
		IEnumerable<object> Containers { get; }
		bool GetIsValid();
	}
}
