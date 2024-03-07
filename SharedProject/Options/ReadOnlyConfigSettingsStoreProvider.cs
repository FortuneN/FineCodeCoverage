using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Options
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IReadOnlyConfigSettingsStoreProvider))]
    internal class ReadOnlyConfigSettingsStoreProvider : IReadOnlyConfigSettingsStoreProvider
    {
        public SettingsStore Provide()
        {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                return settingsManager.GetReadOnlySettingsStore(SettingsScope.Configuration);
            });
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
        }
    }
}
