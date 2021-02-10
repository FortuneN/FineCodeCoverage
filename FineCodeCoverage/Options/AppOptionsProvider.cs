using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IAppOptionsProvider))]
    [Export(typeof(IAppOptionsStorageProvider))]
    internal class AppOptionsProvider : IAppOptionsProvider, IAppOptionsStorageProvider
    {
        private Type AppOptionsType = typeof(AppOptions);
        private readonly ILogger logger;

        public event Action<IAppOptions> OptionsChanged;

        [ImportingConstructor]
        public AppOptionsProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public void RaiseOptionsChanged(IAppOptions appOptions)
        {
            OptionsChanged?.Invoke(appOptions);
        }
        public IAppOptions Get()
        {
            var options = new AppOptions(true);
            LoadSettingsFromStorage(options);
            return options;
        }
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
        public void LoadSettingsFromStorage(AppOptions instance)
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(Vsix.Code))
            {
                settingsStore.CreateCollection(Vsix.Code);
            }

            foreach (var property in AppOptionsType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    if (!settingsStore.PropertyExists(Vsix.Code, property.Name))
                    {
                        continue;
                    }

                    var strValue = settingsStore.GetString(Vsix.Code, property.Name);

                    if (string.IsNullOrWhiteSpace(strValue))
                    {
                        continue;
                    }

                    var objValue = JsonConvert.DeserializeObject(strValue, property.PropertyType);

                    property.SetValue(instance, objValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to load '{property.Name}' setting", exception);
                }
            }
        }
        [SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread")]
        public void SaveSettingsToStorage(AppOptions appOptions)
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(Vsix.Code))
            {
                settingsStore.CreateCollection(Vsix.Code);
            }

            foreach (var property in AppOptionsType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    var objValue = property.GetValue(appOptions);
                    var strValue = JsonConvert.SerializeObject(objValue);

                    settingsStore.SetString(Vsix.Code, property.Name, strValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to save '{property.Name}' setting", exception);
                }
            }
            RaiseOptionsChanged(appOptions);
        }
    }

}
