namespace FineCodeCoverage.Engine.Coverlet
{
    internal interface IFCCCoverletConsoleExecutor : ICoverletConsoleExecutor
    {
        void Initialize(string appDataFolder);
    }
}
