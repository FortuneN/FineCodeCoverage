namespace FineCodeCoverage.Engine.MsTestPlatform
{
    interface IMsTestPlatformUtil
    {
		string MsTestPlatformExePath { get; }
		void Initialize(string appDataFolder, System.Threading.CancellationToken cancellationToken);
	}
}
