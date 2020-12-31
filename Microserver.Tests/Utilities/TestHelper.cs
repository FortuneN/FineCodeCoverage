using CliWrap;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microserver.Tests.Utilities
{
	public static class TestHelper
	{
		public static readonly string CalculateCoverageUrl = "/Coverage/Calculate";
		public static readonly string TestCasesFolder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(TestHelper).Assembly.Location))))), "TestCases");
		
		public static async Task<T> DeserializeAsync<T>(this HttpResponseMessage response)
		{
			var content = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(content);
		}

		public static string GetTestCaseFullPath(string relativePath)
		{
			var result = Path.Combine(TestCasesFolder, relativePath.Replace('/', '\\'));
			return result;
		}

		public static void BuildProject(string projectFilePath)
		{
			Cli
			.Wrap("dotnet")
			.WithArguments($@"build ""{projectFilePath}""")
			.WithValidation(CommandResultValidation.ZeroExitCode)
			.ExecuteAsync()
			.GetAwaiter()
			.GetResult();
		}
	}
}
