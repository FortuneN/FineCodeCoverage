﻿namespace FineCodeCoverage.Options
{
    interface IAppOptionsStorageProvider
    {
        void SaveSettingsToStorage(AppOptions appOptions);
        void LoadSettingsFromStorage(AppOptions instance);
    }


}
