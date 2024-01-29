using Microsoft.VisualStudio.Settings;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Options
{
    [ExcludeFromCodeCoverage]
    internal class VsWritableSettingsStore : IWritableSettingsStore
    {
        private readonly WritableSettingsStore writableSettingsStore;

        public VsWritableSettingsStore(WritableSettingsStore writableSettingsStore)
        {
            this.writableSettingsStore = writableSettingsStore;
        }

        public bool CollectionExists(string collectionPath)
        {
            return writableSettingsStore.CollectionExists(collectionPath);
        }

        public void CreateCollection(string collectionPath)
        {
            writableSettingsStore.CreateCollection(collectionPath);
        }

        public string GetString(string collectionPath, string propertyName)
        {
            return writableSettingsStore.GetString(collectionPath, propertyName);
        }

        public bool PropertyExists(string collectionPath, string propertyName)
        {
            return writableSettingsStore.PropertyExists(collectionPath, propertyName);
        }

        public void SetString(string collectionPath, string propertyName, string value)
        {
            writableSettingsStore.SetString(collectionPath, propertyName, value);
        }

        public void SetStringSafe(string collectionPath, string propertyName, string value)
        {
            if (!CollectionExists(collectionPath))
            {
                CreateCollection(collectionPath);
            }
            SetString(collectionPath, propertyName, value);
        }
    }

}
