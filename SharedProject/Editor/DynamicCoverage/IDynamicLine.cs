using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface IDynamicLine : ILine
    {
        bool IsDirty { get; }
    }
}
