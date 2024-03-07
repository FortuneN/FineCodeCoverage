using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IWritableUserSettingsStoreProvider))]
    internal class WritableUserSettingsStoreProvider : IWritableUserSettingsStoreProvider
    {
        private WritableSettingsStore writableSettingsStore;
        public WritableSettingsStore Provide()
        {
            if (writableSettingsStore == null)
            {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
                writableSettingsStore = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                    return settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                });
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
            }
            return writableSettingsStore;
        }
    }

}
