namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal interface INewCodeTrackerFactory
    {
        INewCodeTracker Create(bool isCSharp);
    }
}
