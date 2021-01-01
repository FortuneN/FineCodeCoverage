using FineCodeCoverage;
using FineCodeCoverage.Core.Model;
using Microserver.Tests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Microserver.Tests.TestCases
{
	[TestFixture]
	public class Case1 : WebApplicationFactory<Startup>
	{
		private string _projectFile;
		private string _testDllFile;
		private HttpClient _httpClient;
		
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_projectFile = TestHelper.GetTestCaseFullPath("Case1\\Core_3_1_NUnitTestProject1\\DotNetCore_3_1_NUnitTestProject1.csproj");
			_testDllFile = TestHelper.GetTestCaseFullPath("Case1\\Core_3_1_NUnitTestProject1\\bin\\Debug\\netcoreapp3.1\\DotNetCore_3_1_NUnitTestProject1.dll");

			TestHelper.BuildProject(_projectFile);
			_httpClient = CreateClient();
		}

		[Test]
		public async Task Test1Async()
		{
			// arrange

			var requestData = new CalculateCoverageRequest
			{
				Projects = new[]
				{
					new CoverageProject
					{
						ProjectFile = _projectFile,
						TestDllFile = _testDllFile
					}
				}
			};

			// act

			var httpRequest = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, MediaTypeNames.Application.Json);
			var httpResponse = await _httpClient.PostAsync(TestHelper.CalculateCoverageUrl, httpRequest);
			var responseData = await httpResponse.DeserializeAsync<CalculateCoverageResponse>();

			// assert

			responseData.ToString();
			Assert.IsTrue(httpResponse.IsSuccessStatusCode);
		}
	}
}
