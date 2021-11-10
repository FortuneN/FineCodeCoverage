using System;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface IAssemblyUtil
    {
		T RunInAssemblyResolvingContext<T>(Func<T> func);

	}
}
