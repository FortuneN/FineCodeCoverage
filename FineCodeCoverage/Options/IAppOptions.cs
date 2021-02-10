namespace FineCodeCoverage.Options
{
    public interface IAppOptions
    {
        bool Enabled { get; }
        string[] Exclude { get; }
        string[] ExcludeByAttribute { get; }
        string[] ExcludeByFile { get; }
        string[] Include { get; }
        bool IncludeTestAssembly { get; }
        bool RunInParallel { get; }
        int RunWhenTestsExceed { get; }
        bool RunWhenTestsFail { get; }
    }
}