namespace FineCodeCoverage.Editor.Management
{
    internal readonly struct FCCEditorFormatDefinitionNames
    {
        public FCCEditorFormatDefinitionNames(
            string covered, string notCovered, string partiallyCovered, string newLines, string dirty, string notIncluded
        )
        {
            this.Covered = covered;
            this.NotCovered = notCovered;
            this.PartiallyCovered = partiallyCovered;
            this.NewLines = newLines;
            this.Dirty = dirty;
            this.NotIncluded = notIncluded;
        }

        public string Covered { get; }
        public string NotCovered { get; }
        public string PartiallyCovered { get; }
        public string NewLines { get; }
        public string Dirty { get; }
        public string NotIncluded { get; }
    }
}
