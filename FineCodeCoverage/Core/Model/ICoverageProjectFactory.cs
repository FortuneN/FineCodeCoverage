namespace FineCodeCoverage.Engine.Model
{
    internal interface ICoverageProjectFactory
    {
		CoverageProject Create();
        void Initialize();
    }
}
