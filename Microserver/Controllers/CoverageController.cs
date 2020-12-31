using FineCodeCoverage.Core;
using FineCodeCoverage.Core.Model;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FineCodeCoverage.Controllers
{
	[ApiController]
	[Route("Coverage")]
	public class CoverageController : ControllerBase
	{
		[HttpPost("Calculate")]
		public async Task<CalculateCoverageResponse> CalculateAsync([FromBody] CalculateCoverageRequest request)
		{
			var response = await FCCEngine.CalculateCoverageAsync(request);
			return response;
		}
	}
}
