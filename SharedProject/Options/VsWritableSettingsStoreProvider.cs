using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IWritableSettingsStoreProvider))]
    internal class VsWritableSettingsStoreProvider : IWritableSettingsStoreProvider
    {
        public IWritableSettingsStore Provide()
        {
            IWritableSettingsStore writableSettingsStore = null;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                var vsWritableSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                writableSettingsStore = new VsWritableSettingsStore(vsWritableSettingsStore);
            });
            return writableSettingsStore;
        }
    }

}
