namespace FineCodeCoverage.Options
{
    interface IAppOptionsStorageProvider
    {
        void SaveSettingsToStorage(IAppOptions appOptions);
        void LoadSettingsFromStorage(IAppOptions instance);
    }


}
