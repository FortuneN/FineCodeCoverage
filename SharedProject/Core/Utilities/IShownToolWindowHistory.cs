namespace FineCodeCoverage.Core.Utilities
{
    interface IShownToolWindowHistory
    {
        bool HasShownToolWindow { get; }
        void ShowedToolWindow();
    }
}
