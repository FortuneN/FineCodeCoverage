using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Impl
{
    interface IDynamicLine : ILine
    {
        bool IsDirty { get; }
    }
}
