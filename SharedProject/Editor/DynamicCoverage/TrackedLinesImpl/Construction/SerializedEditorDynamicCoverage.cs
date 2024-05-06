using System.Collections.Generic;

namespace FineCodeCoverage.Editor.DynamicCoverage.TrackedLinesImpl.Construction
{
    internal class SerializedEditorDynamicCoverage
    {
        public List<SerializedContainingCodeTracker> SerializedContainingCodeTrackers { get; set; }
        public string Text { get; set; }
        public List<int> NewCodeLineNumbers { get; set; }
    }
}
