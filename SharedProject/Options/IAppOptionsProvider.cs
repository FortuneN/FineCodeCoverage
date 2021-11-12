using System;

namespace FineCodeCoverage.Options
{
    internal interface IAppOptionsProvider
    {
        event Action<IAppOptions> OptionsChanged;
        IAppOptions Get();
    }


}
