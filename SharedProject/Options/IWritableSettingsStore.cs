namespace FineCodeCoverage.Options
{
    internal interface IWritableSettingsStore
    {
        bool CollectionExists(string collectionPath);
        void CreateCollection(string collectionPath);
        bool PropertyExists(string collectionPath, string propertyName);
        string GetString(string collectionPath, string propertyName);
        void SetString(string collectionPath, string propertyName, string value);
    }

}
