using System.Collections.Generic;
namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface IContainingCodeTrackerTrackedLines : ITrackedLines
    {
        IReadOnlyList<IContainingCodeTracker> ContainingCodeTrackers { get; }
        INewCodeTracker NewCodeTracker { get; }
    }
}
