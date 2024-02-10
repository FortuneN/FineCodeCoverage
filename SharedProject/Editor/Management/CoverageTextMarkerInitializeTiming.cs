using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(ICoverageTextMarkerInitializeTiming))]
    internal class CoverageTextMarkerInitializeTiming : ICoverageTextMarkerInitializeTiming
    {
        private readonly IEditorFormatMap editorFormatMap;
        private readonly MarkerTypeNames markerTypeNames;

        [ImportingConstructor]
        public CoverageTextMarkerInitializeTiming(
            IEditorFormatMapService editorFormatMapService,
            MarkerTypeNames markerTypeNames
        )
        {
            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            this.markerTypeNames = markerTypeNames;
        }

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
            _ = System.Threading.Tasks.Task.Delay(0).ContinueWith(async (t) =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (initializable.RequiresInitialization)
                {
                    // if not being loaded for the IVsTextMarkerTypeProvider service then this will get vs to ask for the markers
                    var _ = editorFormatMap.GetProperties(markerTypeNames.Covered);
                    // markers available now
                    initializable.Initialize();
                }
            }, TaskScheduler.Default);
        }
    }

}
