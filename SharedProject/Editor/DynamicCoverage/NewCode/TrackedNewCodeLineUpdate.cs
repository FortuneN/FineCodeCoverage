namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal readonly struct TrackedNewCodeLineUpdate
    {
        public TrackedNewCodeLineUpdate(string text, int newLineNumber, int oldLineNumber)
        {
            this.Text = text;
            this.NewLineNumber = newLineNumber;
            this.OldLineNumber = oldLineNumber;
        }
        public string Text { get; }
        public int NewLineNumber { get; }
        public int OldLineNumber { get; }
    }
}
