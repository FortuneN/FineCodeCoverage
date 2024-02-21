namespace FineCodeCoverage.Editor.Management
{
    class MEFItemNames
    {
        public MEFItemNames(string newLinesItemName, string dirtyItemName)
        {
            NewLines = newLinesItemName;
            Dirty = dirtyItemName;
        }
        public string NewLines { get; }
        public string Dirty { get; }
    }
}
