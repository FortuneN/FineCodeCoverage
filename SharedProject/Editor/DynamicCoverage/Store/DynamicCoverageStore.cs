using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Settings;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(IDynamicCoverageStore))]
    internal class DynamicCoverageStore : IDynamicCoverageStore, IListener<NewCoverageLinesMessage>
    {
        private readonly IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider;
        private const string dynamicCoverageStoreCollectionName = "FCC.DynamicCoverageStore";
        private WritableSettingsStore writableUserSettingsStore;
        private WritableSettingsStore WritableUserSettingsStore
        {
            get
            {
                if (writableUserSettingsStore == null)
                {
                    writableUserSettingsStore = writableUserSettingsStoreProvider.Provide();
                }
                return writableUserSettingsStore;
            }
        }

        // todo needs to listen for solution change as well as vs shutdown to clear
        // needs to listen to 	https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivstrackprojectdocuments2.onafterrenamefile?view=visualstudiosdk-2019
        // also the normal coverage needs to listen for file name changed
        [ImportingConstructor]
        public DynamicCoverageStore(
            IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider,
            IFileRenameListener fileRenameListener,
            IEventAggregator eventAggregator
        )
        {
            eventAggregator.AddListener(this);
            this.writableUserSettingsStoreProvider = writableUserSettingsStoreProvider;
            fileRenameListener.ListenForFileRename((oldFileName, newFileName) =>
            {
                var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
                if (collectionExists)
                {
                    if (WritableUserSettingsStore.PropertyExists(dynamicCoverageStoreCollectionName, oldFileName))
                    {
                        var serialized = WritableUserSettingsStore.GetString(dynamicCoverageStoreCollectionName, oldFileName);
                        WritableUserSettingsStore.SetString(dynamicCoverageStoreCollectionName, newFileName, serialized);
                        WritableUserSettingsStore.DeleteProperty(dynamicCoverageStoreCollectionName, oldFileName);
                    }
                }
            });
        }

        public string GetSerializedCoverage(string filePath)
        {
            var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            if (!collectionExists) return null;
            if (WritableUserSettingsStore.PropertyExists(dynamicCoverageStoreCollectionName, filePath))
            {
                return WritableUserSettingsStore.GetString(dynamicCoverageStoreCollectionName, filePath);
            }
            return null;
        }

        public void SaveSerializedCoverage(string filePath,string serializedCoverage)
        {
            var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            if (!collectionExists)
            {
                WritableUserSettingsStore.CreateCollection(dynamicCoverageStoreCollectionName);
            }
            WritableUserSettingsStore.SetString(dynamicCoverageStoreCollectionName, filePath, serializedCoverage);
        }

        public void Handle(NewCoverageLinesMessage message)
        {
            var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            if (collectionExists)
            {
                WritableUserSettingsStore.DeleteCollection(dynamicCoverageStoreCollectionName);
            }
        }

        public void RemoveSerializedCoverage(string filePath)
        {
            WritableUserSettingsStore.DeleteProperty(dynamicCoverageStoreCollectionName, filePath);
        }
    }

}
