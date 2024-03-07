using Microsoft.VisualStudio.Settings;

namespace FineCodeCoverage.Options
{
    internal interface IReadOnlyConfigSettingsStoreProvider
    {
        SettingsStore Provide();
    }
}
