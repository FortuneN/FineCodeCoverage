using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IWritableSettingsStoreProvider))]
    internal class VsWritableSettingsStoreProvider : IWritableSettingsStoreProvider
    {
        private IWritableSettingsStore writableSettingsStore;
        public IWritableSettingsStore Provide()
        {
            if (writableSettingsStore == null)
            {
                writableSettingsStore = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                    var vsWritableSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                    return new VsWritableSettingsStore(vsWritableSettingsStore);
                });
            }
            return writableSettingsStore;
        }
    }

}
