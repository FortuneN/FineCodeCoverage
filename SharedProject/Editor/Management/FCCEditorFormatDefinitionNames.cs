namespace FineCodeCoverage.Editor.Management
{
    internal struct FCCEditorFormatDefinitionNames
    {
        public FCCEditorFormatDefinitionNames(
            string covered, string notCovered, string partiallyCovered, string newLines, string dirty, string notIncluded
        )
        {
            Covered = covered;
            NotCovered = notCovered;
            PartiallyCovered = partiallyCovered;
            NewLines = newLines;
            Dirty = dirty;
            NotIncluded = notIncluded;
        }

        public string Covered { get; }
        public string NotCovered { get; }
        public string PartiallyCovered { get; }
        public string NewLines { get; }
        public string Dirty { get; }
        public string NotIncluded { get; }
    }

}
