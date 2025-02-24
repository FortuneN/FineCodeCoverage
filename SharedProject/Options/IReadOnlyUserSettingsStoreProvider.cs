using Microsoft.VisualStudio.Settings;

namespace FineCodeCoverage.Options
{
    internal interface IReadOnlyUserSettingsStoreProvider
    {
        System.Threading.Tasks.Task<SettingsStore> ProvideAsync();
    }
}
