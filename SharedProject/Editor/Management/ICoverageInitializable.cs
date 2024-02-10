namespace FineCodeCoverage.Editor.Management
{
    interface ICoverageInitializable
    {
        bool RequiresInitialization { get; }
        void Initialize();
    }
}
