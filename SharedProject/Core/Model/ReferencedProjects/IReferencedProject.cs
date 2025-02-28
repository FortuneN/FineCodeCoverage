namespace FineCodeCoverage.Engine.Model
{
    internal interface IReferencedProject
    {
        string AssemblyName { get; }
        bool IsDll { get; }
    }
}
