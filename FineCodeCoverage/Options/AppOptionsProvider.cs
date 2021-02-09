using System.ComponentModel.Composition;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IAppOptionsProvider))]
    internal class AppOptionsProvider : IAppOptionsProvider
    {
        public IAppOptions Get()
        {
			return AppOptions.Get();
		}
    }


}
