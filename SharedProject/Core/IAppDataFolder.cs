namespace FineCodeCoverage.Engine
{
    internal interface IAppDataFolder
    {
        string DirectoryPath { get; }
        void Initialize();
        
    }

}