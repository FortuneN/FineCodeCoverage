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
        private readonly Type AppOptionsType = typeof(AppOptions);
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
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadSettingsFromStorage(options);
            return options;
        }

        private WritableSettingsStore EnsureStore()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(Vsix.Code))
            {
                settingsStore.CreateCollection(Vsix.Code);
            }
            return settingsStore;
        }

        private PropertyInfo[] ReflectProperties()
        {
            return AppOptionsType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
        }

        public void LoadSettingsFromStorage(AppOptions instance)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = EnsureStore();


            foreach (var property in ReflectProperties())
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

        public void SaveSettingsToStorage(AppOptions appOptions)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = EnsureStore();

            foreach (var property in ReflectProperties())
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
