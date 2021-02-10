namespace FineCodeCoverage.Options
{
    public interface IAppOptions
    {
        bool Enabled { get; set; }
        string[] Exclude { get; set; }
        string[] ExcludeByAttribute { get; set; }
        string[] ExcludeByFile { get; set; }
        string[] Include { get; set; }
        bool IncludeTestAssembly { get; set; }
        bool RunInParallel { get; set; }
        int RunWhenTestsExceed { get; set; }
        bool RunWhenTestsFail { get; set; }
    }
}