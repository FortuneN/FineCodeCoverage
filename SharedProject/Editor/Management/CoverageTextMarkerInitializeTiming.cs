using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(ICoverageTextMarkerInitializeTiming))]
    internal class CoverageTextMarkerInitializeTiming : ICoverageTextMarkerInitializeTiming
    {
        private ICoverageInitializable initializable;
        public ICoverageInitializable Initializable {  set {
                initializable = value;
                Execute();
            } 
        }

        private void Execute()
        {
            // if being loaded for the IVsTextMarkerTypeProvider service then this will run after 
            // GetTextMarkerType has been called.

            _ = System.Threading.Tasks.Task.Delay(0).ContinueWith( async _ =>
            {
                // note that this is necessary
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (initializable.RequiresInitialization)
                {
                    initializable.Initialize();
                }
            }, TaskScheduler.Default);
        }
    }

}
