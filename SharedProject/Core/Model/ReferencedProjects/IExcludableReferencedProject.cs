namespace FineCodeCoverage.Engine.Model
{
    interface IExcludableReferencedProject : IReferencedProject
    {
        bool ExcludeFromCodeCoverage { get; }
    }
}
