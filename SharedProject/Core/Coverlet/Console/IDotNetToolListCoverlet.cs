namespace FineCodeCoverage.Engine.Coverlet
{
    internal class CoverletToolDetails
    {
        public string Version { get; set; }
        public string Command { get; set; }
    }

    internal interface IDotNetToolListCoverlet
    {
        CoverletToolDetails Local(string directory);
        CoverletToolDetails Global();
        CoverletToolDetails GlobalToolsPath(string directory);
    }
}
