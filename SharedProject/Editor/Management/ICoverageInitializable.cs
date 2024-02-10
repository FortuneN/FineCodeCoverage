namespace FineCodeCoverage.Impl
{
    interface ICoverageInitializable
    {
        bool RequiresInitialization { get; }
        void Initialize();
    }
}
