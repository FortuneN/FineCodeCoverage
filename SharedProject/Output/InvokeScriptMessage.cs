namespace FineCodeCoverage.Output
{
    internal class InvokeScriptMessage
    {
        public string ScriptName { get; set; }
        public object[] Arguments { get; set; }

        public InvokeScriptMessage(string scriptName)
        {
            ScriptName = scriptName;
        }

        public InvokeScriptMessage(string scriptName, object argument) : this(scriptName, new object[] { argument })
        { }

        public InvokeScriptMessage(string scriptName, object[] arguments) : this(scriptName)
        {
            Arguments = arguments;
        }
    }
}
