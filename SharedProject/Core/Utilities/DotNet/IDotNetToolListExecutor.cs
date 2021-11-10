namespace FineCodeCoverage.Core.Utilities
{
    internal class DotNetToolListExecutionResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
    }

    internal interface IDotNetToolListExecutor
    {
        DotNetToolListExecutionResult Global();
        DotNetToolListExecutionResult GlobalToolsPath(string directory);
        DotNetToolListExecutionResult Local(string directory);
    }
}