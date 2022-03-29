namespace FineCodeCoverage.Options
{
    internal interface IWritableSettingsStoreProvider
    {
        IWritableSettingsStore Provide();
    }
}
