using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Settings;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(IDynamicCoverageStore))]
    internal class DynamicCoverageStore : IDynamicCoverageStore
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
            IFileRenameListener fileRenameListener
        )
        {
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

        public object GetSerializedCoverage(string filePath)
        {
            throw new System.NotImplementedException();
            //var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            //if (!collectionExists) return null;
            //if (WritableUserSettingsStore.PropertyExists(dynamicCoverageStoreCollectionName, filePath))
            //{
            //    var serialized = WritableUserSettingsStore.GetString(dynamicCoverageStoreCollectionName, filePath);
            //    return JsonConvert.DeserializeObject<List<DynamicLine>>(serialized).Cast<IDynamicLine>().ToList();
            //}
            //return null;
        }

        public void SaveSerializedCoverage(string filePath,object obj)
        {
            throw new System.NotImplementedException();
            //var collectionExists = WritableUserSettingsStore.CollectionExists(dynamicCoverageStoreCollectionName);
            //if (!collectionExists)
            //{
            //    WritableUserSettingsStore.CreateCollection(dynamicCoverageStoreCollectionName);
            //}
            //WritableUserSettingsStore.SetString(dynamicCoverageStoreCollectionName, filePath, serialized);
        }
    }

}
