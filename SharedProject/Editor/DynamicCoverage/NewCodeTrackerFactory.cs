using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INewCodeTrackerFactory))]
    internal class NewCodeTrackerFactory : INewCodeTrackerFactory
    {
        public INewCodeTracker Create(bool isCSharp)
        {
            return new NewCodeTracker(isCSharp);
        }
    }
}
