using FineCodeCoverage.Core.Model;

namespace FineCodeCoverage.Core
{
	internal static class FCCEngine
	{
		public static void Initialize()
		{
			// TODO : Start WebServer process
		}

		public static void ClearProcesses()
		{
			// TODO : Call ClearProcesses on WebServer
		}

		public static CalculateCoverageResponse CalculateCoverage(CalculateCoverageRequest request)
		{
			// TODO : Call CalculateCoverage on WebServer

			// TODO : Fixserialization of request.Settings to produce declared public properties only

			request?.ToString();
			return new CalculateCoverageResponse();
		}
	}
}