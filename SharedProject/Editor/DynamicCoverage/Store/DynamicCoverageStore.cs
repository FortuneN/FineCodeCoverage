using System.ComponentModel.Composition;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage.Utilities;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Settings;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(IDynamicCoverageStore))]
    internal class DynamicCoverageStore : IDynamicCoverageStore, IListener<NewCoverageLinesMessage>
    {
        private readonly IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider;
        private readonly IJsonConvertService jsonConvertService;
        private readonly IDateTimeService dateTimeService;
        private const string dynamicCoverageStoreCollectionName = "FCC.DynamicCoverageStore";
        private WritableSettingsStore writableUserSettingsStore;
        private WritableSettingsStore WritableUserSettingsStore
        {
            get
            {
                if (this.writableUserSettingsStore == null)
                {
                    this.writableUserSettingsStore = this.writableUserSettingsStoreProvider.Provide();
                }

                return this.writableUserSettingsStore;
            }
        }

        // when visual studio is closed SolutionEvents AfterClosing event is fired, the FCCEngine
        // will NewCoverageLinesMessage and the store will be removed
        [ImportingConstructor]
        public DynamicCoverageStore(
            IWritableUserSettingsStoreProvider writableUserSettingsStoreProvider,
            IFileRenameListener fileRenameListener,
            IEventAggregator eventAggregator,
            IJsonConvertService jsonConvertService,
            IDateTimeService dateTimeService
        )
        {
            _ = eventAggregator.AddListener(this);
            this.writableUserSettingsStoreProvider = writableUserSettingsStoreProvider;
            this.jsonConvertService = jsonConvertService;
            this.dateTimeService = dateTimeService;
            fileRenameListener.ListenForFileRename((oldFileName, newFileName) =>
            {
                bool collectionExists = this.WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
                if (collectionExists)
                {
                    if (this.WritableUserSettingsStore.PropertyExists(dynamicCoverageStoreCollectionName, oldFileName))
                    {
                        string serialized = this.WritableUserSettingsStore.GetString(dynamicCoverageStoreCollectionName, oldFileName);
                        this.WritableUserSettingsStore.SetString(dynamicCoverageStoreCollectionName, newFileName, serialized);
                        _ = this.WritableUserSettingsStore.DeleteProperty(dynamicCoverageStoreCollectionName, oldFileName);
                    }
                }
            });
        }

        public SerializedCoverageWhen GetSerializedCoverage(string filePath)
        {
            bool collectionExists = this.WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            return !collectionExists
                ? null
                : this.WritableUserSettingsStore.PropertyExists(dynamicCoverageStoreCollectionName, filePath)
                ? this.jsonConvertService.DeserializeObject<SerializedCoverageWhen>(
                    this.WritableUserSettingsStore.GetString(dynamicCoverageStoreCollectionName, filePath))
                : null;
        }

        public void SaveSerializedCoverage(string filePath, string serializedCoverage)
        {
            bool collectionExists = this.WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            if (!collectionExists)
            {
                this.WritableUserSettingsStore.CreateCollection(dynamicCoverageStoreCollectionName);
            }

            var serializedCoverageWhen = new SerializedCoverageWhen
            {
                Serialized = serializedCoverage,
                When = this.dateTimeService.Now
            };
            string toSerialize = this.jsonConvertService.SerializeObject(serializedCoverageWhen);
            this.WritableUserSettingsStore.SetString(dynamicCoverageStoreCollectionName, filePath, toSerialize);
        }

        // this is fundamental - the store is for restoring the coverage of the current coverage only
        public void Handle(NewCoverageLinesMessage message) => this.RemoveStore();

        private void RemoveStore()
        {
            bool collectionExists = this.WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            if (collectionExists)
            {
                _ = this.WritableUserSettingsStore.DeleteCollection(dynamicCoverageStoreCollectionName);
            }
        }

        public void RemoveSerializedCoverage(string filePath)
            => _ = this.WritableUserSettingsStore.DeleteProperty(dynamicCoverageStoreCollectionName, filePath);
    }
}
