using FineCodeCoverage.Core.Coverlet;

namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface IRunSettingsCoverletConfigurationFactory
    {
        IRunSettingsCoverletConfiguration Create();
    }
}
