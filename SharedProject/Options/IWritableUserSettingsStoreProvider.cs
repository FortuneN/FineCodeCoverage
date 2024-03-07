using Microsoft.VisualStudio.Settings;

namespace FineCodeCoverage.Options
{
    internal interface IWritableUserSettingsStoreProvider
    {
        WritableSettingsStore Provide();
    }
}
