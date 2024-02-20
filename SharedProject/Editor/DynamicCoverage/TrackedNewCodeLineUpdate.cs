namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal readonly struct TrackedNewCodeLineUpdate
    {
        public TrackedNewCodeLineUpdate(string text, int lineNumber, bool lineUpdated)
        {
            Text = text;
            LineNumber = lineNumber;
            LineUpdated = lineUpdated;
        }
        public string Text { get; }
        public int LineNumber { get; }
        public bool LineUpdated { get; }
    }
}
